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

// Listens for cart checkouts and deducts the sold quantities from stock. Idempotent: a Reservation
// row keyed by CartId records that the checkout was already applied, so redelivered events are skipped.
public class CartCheckedOutConsumer(
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    ILogger<CartCheckedOutConsumer> logger) : BackgroundService
{
    private const string Topic = "cart-checkedout";
    private const string DlqTopic = "dlq.cart-checkedout";
    private const string ConsumerGroup = "inventory-checkout-consumer";
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
        logger.LogInformation("CartCheckedOutConsumer started. Topic: {Topic}, Group: {Group}", Topic, ConsumerGroup);

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
                logger.LogError(ex, "Unexpected error in CartCheckedOutConsumer loop.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        consumer.Close();
        logger.LogInformation("CartCheckedOutConsumer stopped.");
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
                    logger.LogError(ex, "CartCheckedOut processing failed after {Max} retries. -> DLQ.", MaxRetries);
                    await dlq.ProduceAsync(DlqTopic, new Message<string, string> { Key = result.Message.Key, Value = result.Message.Value }, ct);
                    return;
                }
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);
            }
        }
    }

    private async Task HandleAsync(string value, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<CartCheckedOutEvent>(value);
        if (evt is null) { logger.LogWarning("Could not deserialize CartCheckedOutEvent."); return; }

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var lockService = scope.ServiceProvider.GetRequiredService<IDistributedLockService>();

        if (await db.Reservations.AnyAsync(r => r.CartId == evt.CartId, ct))
        {
            logger.LogInformation("Checkout {CartId} already applied to inventory. Skipping.", evt.CartId);
            return;
        }

        // Lock every SKU being sold (sorted order, avoids deadlocking against a concurrent
        // reservation/restock touching an overlapping set of products) so no other consumer instance
        // can read/write the same InventoryItem rows until this checkout is fully saved.
        var lockKeys = evt.Items.Select(i => $"inventory:{i.ProductId}");
        await using var stockLock = await lockService.AcquireAllAsync(lockKeys, LockExpiry, LockWait, ct)
            ?? throw new InvalidOperationException($"Could not acquire inventory lock for checkout {evt.CartId}.");

        foreach (var item in evt.Items)
        {
            var inv = await db.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == item.ProductId, ct);
            if (inv is null)
            {
                logger.LogWarning("No inventory record for ProductId {ProductId} on checkout {CartId}.", item.ProductId, evt.CartId);
                continue;
            }
            inv.Sell(item.Quantity);
        }

        var reservation = Reservation.Create(
            evt.CartId, evt.UserId, evt.CartId.ToString(),
            evt.Items.Select(i => new ReservationItem { ProductId = i.ProductId, Sku = i.Sku, Quantity = i.Quantity, UnitPrice = i.UnitPrice }));
        reservation.Confirm();
        await db.Reservations.AddAsync(reservation, ct);

        await db.SaveChangesAsync(ct);

        // Invalidate the cached inventory for each affected product so reads reflect the new stock.
        var cache = scope.ServiceProvider.GetRequiredService<IInventoryCacheService>();
        foreach (var item in evt.Items)
            await cache.RemoveInventoryAsync(item.ProductId, ct);

        logger.LogInformation("Inventory deducted for checkout {CartId} ({Count} items).", evt.CartId, evt.Items.Count);
    }
}
