using System.Text;
using System.Text.Json;
using CommerceSphere.NotificationService.Application.Interfaces;
using CommerceSphere.Shared.Contracts.Events.Auth;
using CommerceSphere.Shared.Contracts.Events.Cart;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CommerceSphere.NotificationService.Infrastructure.Kafka.Consumers;

// Single consumer for everything the notification service reacts to. Subscribes to the order +
// user topics, dispatches each message to the idempotent handler, and retries with backoff before
// dead-lettering a poison message so one bad message can't block the partition forever.
//
// Reliability: EnableAutoCommit=false + Commit only after success means a message is re-delivered
// (never lost) if the service crashes or is down — when it comes back up it resumes from the last
// committed offset and catches up. AutoOffsetReset.Earliest lets a brand-new consumer group replay
// history (so existing users are backfilled into the contacts read-model).
public class NotificationConsumer(
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    ILogger<NotificationConsumer> logger) : BackgroundService
{
    private const string CheckedOutTopic = "cart-checkedout";
    private const string CancelledTopic = "cart-cancelled";
    private const string UserCreatedTopic = "user-created";
    private const string DlqTopic = "dlq.notifications";
    private const string ConsumerGroup = "notification-service";
    private const int MaxRetries = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        var bootstrap = config["Kafka:BootstrapServers"] ?? "localhost:9092";
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = bootstrap,
            GroupId = ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };
        var producerConfig = new ProducerConfig { BootstrapServers = bootstrap };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        using var dlqProducer = new ProducerBuilder<string, string>(producerConfig).Build();
        consumer.Subscribe([CheckedOutTopic, CancelledTopic, UserCreatedTopic]);
        logger.LogInformation("NotificationConsumer started. Group: {Group}", ConsumerGroup);

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
                logger.LogError(ex, "Unexpected error in NotificationConsumer loop.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        consumer.Close();
        logger.LogInformation("NotificationConsumer stopped.");
    }

    private async Task ProcessWithRetryAsync(ConsumeResult<string, string> result, IProducer<string, string> dlq, CancellationToken ct)
    {
        var attempt = 0;
        while (attempt < MaxRetries)
        {
            try { await DispatchAsync(result.Topic, result.Message.Value, ct); return; }
            catch (Exception ex)
            {
                attempt++;
                if (attempt >= MaxRetries)
                {
                    logger.LogError(ex, "Message from {Topic} failed after {Max} retries -> DLQ.", result.Topic, MaxRetries);
                    var msg = new Message<string, string>
                    {
                        Key = result.Message.Key,
                        Value = result.Message.Value,
                        Headers = [new Header("source-topic", Encoding.UTF8.GetBytes(result.Topic))]
                    };
                    await dlq.ProduceAsync(DlqTopic, msg, ct);
                    return;
                }
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);
            }
        }
    }

    private async Task DispatchAsync(string topic, string value, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IOrderEventHandler>();

        switch (topic)
        {
            case CheckedOutTopic:
                var placed = JsonSerializer.Deserialize<CartCheckedOutEvent>(value)
                    ?? throw new InvalidOperationException("Bad CartCheckedOutEvent.");
                await handler.HandleCheckedOutAsync(placed, ct);
                break;

            case CancelledTopic:
                var cancelled = JsonSerializer.Deserialize<CartCancelledEvent>(value)
                    ?? throw new InvalidOperationException("Bad CartCancelledEvent.");
                await handler.HandleCancelledAsync(cancelled, ct);
                break;

            case UserCreatedTopic:
                var user = JsonSerializer.Deserialize<UserCreatedEvent>(value)
                    ?? throw new InvalidOperationException("Bad UserCreatedEvent.");
                await handler.HandleUserCreatedAsync(user, ct);
                break;

            default:
                logger.LogWarning("Ignoring message from unexpected topic {Topic}.", topic);
                break;
        }
    }
}
