using System.Text.Json;
using CommerceSphere.InventoryService.Application.Interfaces;
using CommerceSphere.InventoryService.Domain.Entities;
using CommerceSphere.InventoryService.Infrastructure.Data;
using CommerceSphere.Shared.Common.Locking;
using CommerceSphere.Shared.Contracts.Events.Cart;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CommerceSphere.InventoryService.Infrastructure.Kafka.Consumers;

// When an order is cancelled, return the sold quantities to stock. Idempotent: the checkout's
// Reservation row is flipped to Cancelled, so a replayed event finds it already cancelled and skips.
public class CartCancelledConsumer(
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    ILogger<CartCancelledConsumer> logger) : BackgroundService
{
    private const string Topic = "cart-cancelled";
    private const string DlqTopic = "dlq.cart-cancelled";
    private const string ConsumerGroup = "inventory-cancel-consumer";
    private const int MaxRetries = 3;
    private static readonly TimeSpan LockExpiry = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan LockWait = TimeSpan.FromSeconds(5);

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
        logger.LogInformation("CartCancelledConsumer started. Topic: {Topic}, Group: {Group}", Topic, ConsumerGroup);

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
                logger.LogError(ex, "Unexpected error in CartCancelledConsumer loop.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        consumer.Close();
        logger.LogInformation("CartCancelledConsumer stopped.");
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
                    logger.LogError(ex, "Cancel restock failed after {Max} retries. -> DLQ.", MaxRetries);
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

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var lockService = scope.ServiceProvider.GetRequiredService<IDistributedLockService>();

        var reservation = await db.Reservations.FirstOrDefaultAsync(r => r.CartId == evt.CartId, ct);
        if (reservation is null)
        {
            logger.LogWarning("No reservation for cancelled cart {CartId}; nothing to restock.", evt.CartId);
            return;
        }
        if (reservation.Status != ReservationStatus.Confirmed)
        {
            logger.LogInformation("Cancel for cart {CartId} already restocked (status {Status}). Skipping.", evt.CartId, reservation.Status);
            return;
        }

        var lockKeys = evt.Items.Select(i => $"inventory:{i.ProductId}");
        await using var stockLock = await lockService.AcquireAllAsync(lockKeys, LockExpiry, LockWait, ct)
            ?? throw new InvalidOperationException($"Could not acquire inventory lock for cancellation {evt.CartId}.");

        foreach (var item in evt.Items)
        {
            var inv = await db.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == item.ProductId, ct);
            inv?.ReceiveStock(item.Quantity);
        }
        reservation.Cancel();
        await db.SaveChangesAsync(ct);

        var cache = scope.ServiceProvider.GetRequiredService<IInventoryCacheService>();
        foreach (var item in evt.Items)
            await cache.RemoveInventoryAsync(item.ProductId, ct);

        logger.LogInformation("Inventory restocked for cancelled order {CartId} ({Count} items).", evt.CartId, evt.Items.Count);
    }
}
