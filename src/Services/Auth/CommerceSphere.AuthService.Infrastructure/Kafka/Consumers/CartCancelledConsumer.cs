using System.Text.Json;
using CommerceSphere.AuthService.Application.Interfaces;
using CommerceSphere.AuthService.Domain.Interfaces;
using CommerceSphere.Shared.Contracts.Events.Cart;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CommerceSphere.AuthService.Infrastructure.Kafka.Consumers;

// Emails the customer when their order is cancelled. Auth owns both the user records (emails) and
// the email sender, so it's the natural place to send the notification. Idempotent via a Redis guard.
public class CartCancelledConsumer(
    IServiceScopeFactory scopeFactory,
    IConnectionMultiplexer redis,
    IConfiguration config,
    ILogger<CartCancelledConsumer> logger) : BackgroundService
{
    private const string Topic = "cart-cancelled";
    private const string DlqTopic = "dlq.cart-cancelled";
    private const string ConsumerGroup = "auth-cancel-email-consumer";
    private const int MaxRetries = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = config["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };
        var producerConfig = new ProducerConfig { BootstrapServers = config["Kafka:BootstrapServers"] ?? "localhost:9092" };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        using var dlqProducer = new ProducerBuilder<string, string>(producerConfig).Build();
        consumer.Subscribe(Topic);
        logger.LogInformation("Auth CartCancelledConsumer started. Topic: {Topic}, Group: {Group}", Topic, ConsumerGroup);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                if (result?.Message?.Value is null) continue;
                await ProcessWithRetryAsync(result, dlqProducer, stoppingToken);
                consumer.Commit(result);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in Auth CartCancelledConsumer loop.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        consumer.Close();
        logger.LogInformation("Auth CartCancelledConsumer stopped.");
    }

    private async Task ProcessWithRetryAsync(ConsumeResult<string, string> result, IProducer<string, string> dlq, CancellationToken ct)
    {
        var attempt = 0;
        while (attempt < MaxRetries)
        {
            try { await HandleAsync(result.Message.Value, ct); return; }
            catch (Exception ex)
            {
                attempt++;
                if (attempt >= MaxRetries)
                {
                    logger.LogError(ex, "Cancellation email failed after {Max} retries. -> DLQ.", MaxRetries);
                    await dlq.ProduceAsync(DlqTopic, new Message<string, string> { Key = result.Message.Key, Value = result.Message.Value }, ct);
                    return;
                }
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);
            }
        }
    }

    private async Task HandleAsync(string value, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<CartCancelledEvent>(value);
        if (evt is null) { logger.LogWarning("Could not deserialize CartCancelledEvent."); return; }

        var guardKey = $"email:order-cancelled:{evt.CartId}";
        var redisDb = redis.GetDatabase();
        if (!await redisDb.StringSetAsync(guardKey, "1", TimeSpan.FromDays(7), When.NotExists))
        {
            logger.LogInformation("Cancellation email for order {CartId} already sent. Skipping.", evt.CartId);
            return;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var email = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var user = await uow.Users.GetByIdAsync(evt.UserId, ct);
            if (user is null)
            {
                logger.LogWarning("No user {UserId} for cancelled order {CartId}; cannot email.", evt.UserId, evt.CartId);
                return;
            }

            var orderRef = "#" + evt.CartId.ToString()[..8].ToUpperInvariant();
            await email.SendOrderCancelledAsync(user.Email, user.FirstName, orderRef, evt.Reason, ct);
            logger.LogInformation("Cancellation email sent to {Email} for order {OrderRef}.", user.Email, orderRef);
        }
        catch
        {
            await redisDb.KeyDeleteAsync(guardKey);
            throw;
        }
    }
}
