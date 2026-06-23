using System.Text.Json;
using CommerceSphere.CartService.Application.Interfaces;
using CommerceSphere.Shared.Contracts.Events.Inventory;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CommerceSphere.CartService.Infrastructure.Kafka.Consumers;

public class InventorySagaConsumer(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<InventorySagaConsumer> logger) : BackgroundService
{
    private const string InventoryReservedTopic = "inventory-reserved";
    private const string InventoryReservationFailedTopic = "inventory-reservation-failed";
    private const string ConsumerGroup = "cart-inventory-saga-consumer";
    private const string DlqTopic = "dlq.cart-checkedout";
    private const int MaxRetries = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Yield immediately so BackgroundService.StartAsync returns before consumer.Consume() blocks
        await Task.Yield();

        var config = new ConsumerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "kafka:9092",
            GroupId = ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe([InventoryReservedTopic, InventoryReservationFailedTopic]);

        logger.LogInformation("InventorySagaConsumer started. Subscribed to {Topics}",
            string.Join(", ", InventoryReservedTopic, InventoryReservationFailedTopic));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                if (result is null) continue;

                await ProcessWithRetryAsync(result, stoppingToken);
                consumer.Commit(result);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("InventorySagaConsumer shutting down.");
                break;
            }
            catch (ConsumeException ex)
            {
                logger.LogError(ex, "Consume error: {Reason}", ex.Error.Reason);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in InventorySagaConsumer loop.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        consumer.Close();
    }

    private async Task ProcessWithRetryAsync(ConsumeResult<string, string> result, CancellationToken ct)
    {
        var attempts = 0;
        while (true)
        {
            try
            {
                attempts++;
                await HandleMessageAsync(result, ct);
                return;
            }
            catch (Exception ex) when (attempts < MaxRetries)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempts));
                logger.LogWarning(ex,
                    "Message processing failed (attempt {Attempt}/{Max}). Retrying in {Delay}s. Topic={Topic} Partition={Partition} Offset={Offset}",
                    attempts, MaxRetries, delay.TotalSeconds, result.Topic, result.Partition, result.Offset);
                await Task.Delay(delay, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Message permanently failed after {Max} attempts. Sending to DLQ {DlqTopic}. Topic={Topic} Partition={Partition} Offset={Offset}",
                    MaxRetries, DlqTopic, result.Topic, result.Partition, result.Offset);
                await SendToDlqAsync(result, ex);
                return;
            }
        }
    }

    private async Task HandleMessageAsync(ConsumeResult<string, string> result, CancellationToken ct)
    {
        logger.LogDebug("Processing message from topic {Topic}: {Value}", result.Topic, result.Message.Value);

        using var scope = scopeFactory.CreateScope();
        var cartManager = scope.ServiceProvider.GetRequiredService<ICartManager>();

        if (result.Topic == InventoryReservationFailedTopic)
        {
            var @event = JsonSerializer.Deserialize<InventoryReservationFailedEvent>(result.Message.Value)
                ?? throw new InvalidOperationException("Failed to deserialize InventoryReservationFailedEvent.");

            logger.LogWarning(
                "Saga compensation triggered: inventory reservation failed for CartId={CartId}. Reason={Reason}. CorrelationId={CorrelationId}",
                @event.CartId, @event.Reason, @event.CorrelationId);

            await cartManager.RollbackAsync(@event.CartId, @event.Reason, ct);

            logger.LogInformation("Saga compensation complete: cart {CartId} rolled back.", @event.CartId);
        }
        else if (result.Topic == InventoryReservedTopic)
        {
            var @event = JsonSerializer.Deserialize<InventoryReservedEvent>(result.Message.Value)
                ?? throw new InvalidOperationException("Failed to deserialize InventoryReservedEvent.");

            logger.LogInformation(
                "Saga step 2 complete: inventory reserved for CartId={CartId}. ReservationId={ReservationId}. CorrelationId={CorrelationId}",
                @event.CartId, @event.ReservationId, @event.CorrelationId);
        }
    }

    private async Task SendToDlqAsync(ConsumeResult<string, string> result, Exception ex)
    {
        var dlqConfig = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "kafka:9092"
        };

        try
        {
            using var producer = new ProducerBuilder<string, string>(dlqConfig).Build();
            var dlqMessage = new Message<string, string>
            {
                Key = result.Message.Key,
                Value = result.Message.Value,
                Headers = new Headers
                {
                    { "original-topic", System.Text.Encoding.UTF8.GetBytes(result.Topic) },
                    { "error", System.Text.Encoding.UTF8.GetBytes(ex.Message) },
                    { "failed-at", System.Text.Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")) }
                }
            };

            await producer.ProduceAsync(DlqTopic, dlqMessage);
            logger.LogInformation("Message sent to DLQ topic {DlqTopic}", DlqTopic);
        }
        catch (Exception dlqEx)
        {
            logger.LogError(dlqEx, "Failed to send message to DLQ {DlqTopic}", DlqTopic);
        }
    }
}
