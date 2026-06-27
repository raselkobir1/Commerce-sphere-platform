using System.Text.Json;
using CommerceSphere.ProductService.Application.Interfaces;
using CommerceSphere.ProductService.Infrastructure.Data;
using CommerceSphere.Shared.Contracts.Events.Cart;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CommerceSphere.ProductService.Infrastructure.Kafka.Consumers;

// Keeps the catalog's Stock figure in sync when a cart is checked out. Idempotent via a Redis
// guard key per CartId so a redelivered event won't double-decrement.
public class CartCheckedOutConsumer(
    IServiceScopeFactory scopeFactory,
    IConnectionMultiplexer redis,
    IConfiguration config,
    ILogger<CartCheckedOutConsumer> logger) : BackgroundService
{
    private const string Topic = "cart-checkedout";
    private const string DlqTopic = "dlq.cart-checkedout";
    private const string ConsumerGroup = "product-checkout-consumer";
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
        logger.LogInformation("Product CartCheckedOutConsumer started. Topic: {Topic}, Group: {Group}", Topic, ConsumerGroup);

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
                logger.LogError(ex, "Unexpected error in Product CartCheckedOutConsumer loop.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        consumer.Close();
        logger.LogInformation("Product CartCheckedOutConsumer stopped.");
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
                    logger.LogError(ex, "Product checkout processing failed after {Max} retries. -> DLQ.", MaxRetries);
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

        var db_key = $"checkout:product-applied:{evt.CartId}";
        var redisDb = redis.GetDatabase();

        // Acquire the idempotency guard; if it's already set, this checkout was applied.
        if (!await redisDb.StringSetAsync(db_key, "1", TimeSpan.FromDays(7), When.NotExists))
        {
            logger.LogInformation("Checkout {CartId} already applied to catalog stock. Skipping.", evt.CartId);
            return;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

            var affected = new List<Guid>();
            foreach (var item in evt.Items)
            {
                var product = await db.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId, ct);
                if (product is null) continue;
                product.DecreaseStock(item.Quantity);
                affected.Add(product.Id);
            }

            await db.SaveChangesAsync(ct);

            // Evict the cached product so the catalog/detail show the reduced stock immediately.
            var cache = scope.ServiceProvider.GetRequiredService<IProductCacheService>();
            foreach (var id in affected)
                await cache.RemoveProductAsync(id, ct);

            logger.LogInformation("Catalog stock decremented for checkout {CartId}.", evt.CartId);
        }
        catch
        {
            // Release the guard so a retry can re-apply this checkout.
            await redisDb.KeyDeleteAsync(db_key);
            throw;
        }
    }
}
