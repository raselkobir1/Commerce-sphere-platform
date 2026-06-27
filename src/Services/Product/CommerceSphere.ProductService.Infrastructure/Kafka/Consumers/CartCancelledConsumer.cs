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

// Returns catalog Stock when an order is cancelled. Idempotent via a Redis guard per CartId.
public class CartCancelledConsumer(
    IServiceScopeFactory scopeFactory,
    IConnectionMultiplexer redis,
    IConfiguration config,
    ILogger<CartCancelledConsumer> logger) : BackgroundService
{
    private const string Topic = "cart-cancelled";
    private const string DlqTopic = "dlq.cart-cancelled";
    private const string ConsumerGroup = "product-cancel-consumer";
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
        logger.LogInformation("Product CartCancelledConsumer started. Topic: {Topic}, Group: {Group}", Topic, ConsumerGroup);

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
                logger.LogError(ex, "Unexpected error in Product CartCancelledConsumer loop.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        consumer.Close();
        logger.LogInformation("Product CartCancelledConsumer stopped.");
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
                    logger.LogError(ex, "Product cancel restock failed after {Max} retries. -> DLQ.", MaxRetries);
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

        var guardKey = $"checkout:product-cancelled:{evt.CartId}";
        var redisDb = redis.GetDatabase();
        if (!await redisDb.StringSetAsync(guardKey, "1", TimeSpan.FromDays(7), When.NotExists))
        {
            logger.LogInformation("Cancel for cart {CartId} already restocked in catalog. Skipping.", evt.CartId);
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
                product.IncreaseStock(item.Quantity);
                affected.Add(product.Id);
            }
            await db.SaveChangesAsync(ct);

            var cache = scope.ServiceProvider.GetRequiredService<IProductCacheService>();
            foreach (var id in affected)
                await cache.RemoveProductAsync(id, ct);

            logger.LogInformation("Catalog stock restored for cancelled order {CartId}.", evt.CartId);
        }
        catch
        {
            await redisDb.KeyDeleteAsync(guardKey);
            throw;
        }
    }
}
