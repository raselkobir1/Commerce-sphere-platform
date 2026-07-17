using System.Text.Json;
using CommerceSphere.CartService.Application.Interfaces;
using CommerceSphere.Shared.Common.Idempotency;
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
    // Consumer group ensures that if multiple Cart Service instances are running, each message
    // is only processed by one of them (Kafka partitions are distributed across the group).
    private const string ConsumerGroup = "cart-inventory-saga-consumer";
    // Dead Letter Queue: messages that fail all retries land here for manual inspection/replay.
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
            // Earliest: on first run (no committed offset yet) start from the beginning of
            // the topic so no events are missed.
            AutoOffsetReset = AutoOffsetReset.Earliest,
            // Manual commit (after successful processing) gives us at-least-once delivery:
            // if the process crashes mid-handling, the offset is not advanced and the
            // message will be redelivered rather than silently lost.
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
                // Exponential back-off (2^attempt seconds): 2s → 4s → 8s reduces pressure
                // on a struggling downstream service compared to fixed-interval retries.
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempts));
                logger.LogWarning(ex,
                    "Message processing failed (attempt {Attempt}/{Max}). Retrying in {Delay}s. Topic={Topic} Partition={Partition} Offset={Offset}",
                    attempts, MaxRetries, delay.TotalSeconds, result.Topic, result.Partition, result.Offset);
                await Task.Delay(delay, ct);
            }
            catch (Exception ex)
            {
                // All retries exhausted — send to DLQ so the offset can still be committed
                // and the consumer keeps moving without getting permanently stuck on one bad message.
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

        // BackgroundService is a singleton but ICartManager/IIdempotencyService are scoped.
        // Creating a new DI scope per message is the standard way to resolve scoped services
        // inside a singleton without causing lifetime issues.
        using var scope = scopeFactory.CreateScope();
        var cartManager = scope.ServiceProvider.GetRequiredService<ICartManager>();
        var idempotencyService = scope.ServiceProvider.GetRequiredService<IIdempotencyService>();

        if (result.Topic == InventoryReservationFailedTopic)
        {
            var @event = JsonSerializer.Deserialize<InventoryReservationFailedEvent>(result.Message.Value)
                ?? throw new InvalidOperationException("Failed to deserialize InventoryReservationFailedEvent.");

            // Guard against redelivery (e.g. a crash between processing and committing the Kafka
            // offset): without this, a duplicate failure event would call RollbackAsync again even
            // if the cart had already moved on to a different terminal state.
            if (!await idempotencyService.TryMarkProcessedAsync($"saga:reservation-failed:{@event.CartId}", TimeSpan.FromDays(7), ct))
            {
                logger.LogInformation("Reservation-failed event for cart {CartId} already handled. Skipping.", @event.CartId);
                return;
            }

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

            if (!await idempotencyService.TryMarkProcessedAsync($"saga:reserved:{@event.CartId}", TimeSpan.FromDays(7), ct))
            {
                logger.LogInformation("Reserved event for cart {CartId} already handled. Skipping.", @event.CartId);
                return;
            }

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
                // Attach diagnostic headers so engineers can identify where the failure came from
                // and replay the message without losing context.
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
