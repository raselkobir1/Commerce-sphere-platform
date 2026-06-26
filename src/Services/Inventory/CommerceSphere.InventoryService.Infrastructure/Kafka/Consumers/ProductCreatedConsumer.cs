using System.Text.Json;
using CommerceSphere.InventoryService.Domain.Entities;
using CommerceSphere.InventoryService.Infrastructure.Data;
using CommerceSphere.Shared.Contracts.Events.Product;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CommerceSphere.InventoryService.Infrastructure.Kafka.Consumers;

public class ProductCreatedConsumer(
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    ILogger<ProductCreatedConsumer> logger) : BackgroundService
{
    private const string Topic = "product-created";
    private const string DlqTopic = "dlq.product-created";
    private const string ConsumerGroup = "inventory-product-consumer";
    private const int MaxRetries = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Yield immediately so BackgroundService.StartAsync returns before consumer.Consume() blocks
        await Task.Yield();

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = config["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = config["Kafka:BootstrapServers"] ?? "localhost:9092"
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        using var dlqProducer = new ProducerBuilder<string, string>(producerConfig).Build();

        consumer.Subscribe(Topic);

        logger.LogInformation(
            "ProductCreatedConsumer started. Topic: {Topic}, Group: {Group}", Topic, ConsumerGroup);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                if (result?.Message?.Value is null)
                    continue;

                await ProcessWithRetryAsync(result, dlqProducer, stoppingToken);
                consumer.Commit(result);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in ProductCreatedConsumer loop.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        consumer.Close();
        logger.LogInformation("ProductCreatedConsumer stopped.");
    }

    private async Task ProcessWithRetryAsync(
        ConsumeResult<string, string> result,
        IProducer<string, string> dlqProducer,
        CancellationToken ct)
    {
        var attempt = 0;
        while (attempt < MaxRetries)
        {
            try
            {
                await HandleMessageAsync(result.Message.Value, ct);
                return;
            }
            catch (Exception ex)
            {
                attempt++;
                if (attempt >= MaxRetries)
                {
                    logger.LogError(ex,
                        "Failed to process ProductCreatedEvent after {MaxRetries} retries. Sending to DLQ. Key: {Key}",
                        MaxRetries, result.Message.Key);

                    await dlqProducer.ProduceAsync(DlqTopic, new Message<string, string>
                    {
                        Key = result.Message.Key,
                        Value = result.Message.Value
                    }, ct);

                    return;
                }

                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                logger.LogWarning(ex,
                    "Error processing ProductCreatedEvent. Retry {Attempt}/{MaxRetries} in {Delay}s.",
                    attempt, MaxRetries, delay.TotalSeconds);

                await Task.Delay(delay, ct);
            }
        }
    }

    private async Task HandleMessageAsync(string messageValue, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<ProductCreatedEvent>(messageValue);
        if (evt is null)
        {
            logger.LogWarning("Failed to deserialize ProductCreatedEvent. Value: {Value}", messageValue);
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        // Check if inventory item already exists for this product (idempotent)
        var exists = await db.InventoryItems.AnyAsync(i => i.ProductId == evt.ProductId, ct);
        if (exists)
        {
            logger.LogInformation(
                "InventoryItem already exists for ProductId: {ProductId}. Skipping.", evt.ProductId);
            return;
        }

        var inventoryItem = InventoryItem.Create(
            productId: evt.ProductId,
            sku: evt.Sku,
            quantityOnHand: evt.InitialStock,
            // Default reorder level of 10 — warehouse team adjusts per-SKU via the adjust-stock endpoint.
            reorderLevel: 10);

        await db.InventoryItems.AddAsync(inventoryItem, ct);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "InventoryItem created from ProductCreatedEvent. ProductId: {ProductId}, SKU: {Sku}, InitialStock: {InitialStock}",
            evt.ProductId, evt.Sku, evt.InitialStock);
    }
}
