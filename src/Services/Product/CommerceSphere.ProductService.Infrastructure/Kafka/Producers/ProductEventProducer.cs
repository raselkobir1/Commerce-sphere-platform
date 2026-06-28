using System.Text;
using System.Text.Json;
using CommerceSphere.ProductService.Application.Interfaces;
using CommerceSphere.Shared.Common.Resilience;
using CommerceSphere.Shared.Contracts.Events.Product;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CommerceSphere.ProductService.Infrastructure.Kafka.Producers;

public class ProductEventProducer(IConfiguration config, ILogger<ProductEventProducer> logger)
    : IProductEventProducer, IDisposable
{
    private readonly IProducer<string, string> _producer = new ProducerBuilder<string, string>(
        new ProducerConfig
        {
            BootstrapServers = config["Kafka:BootstrapServers"] ?? "localhost:9092"
        }).Build();

    public async Task PublishProductCreatedAsync(ProductCreatedEvent evt, CancellationToken ct = default)
    {
        var policy = ResiliencePolicies.KafkaRetryPolicy(logger);
        await policy.ExecuteAsync(async () =>
        {
            var message = new Message<string, string>
            {
                Key = evt.ProductId.ToString(),
                Value = JsonSerializer.Serialize(evt),
                Headers = new Headers
                {
                    { "event-type", Encoding.UTF8.GetBytes("ProductCreatedEvent") },
                    { "version", Encoding.UTF8.GetBytes(evt.Version.ToString()) },
                    { "correlation-id", Encoding.UTF8.GetBytes(evt.CorrelationId) }
                }
            };

            await _producer.ProduceAsync("product-created", message, ct);
            logger.LogInformation(
                "Published ProductCreatedEvent. ProductId: {ProductId}, SKU: {Sku}",
                evt.ProductId, evt.Sku);
        });
    }

    public Task PublishProductCreatedBatchAsync(IEnumerable<ProductCreatedEvent> events, CancellationToken ct = default)
    {
        var count = 0;
        foreach (var evt in events)
        {
            ct.ThrowIfCancellationRequested();

            var message = new Message<string, string>
            {
                Key = evt.ProductId.ToString(),
                Value = JsonSerializer.Serialize(evt),
                Headers = new Headers
                {
                    { "event-type", Encoding.UTF8.GetBytes("ProductCreatedEvent") },
                    { "version", Encoding.UTF8.GetBytes(evt.Version.ToString()) },
                    { "correlation-id", Encoding.UTF8.GetBytes(evt.CorrelationId) }
                }
            };

            // Non-blocking enqueue: the delivery handler only logs failures. Throughput here is
            // bounded by librdkafka's internal queue, not by per-message await round-trips.
            _producer.Produce("product-created", message, report =>
            {
                if (report.Error.IsError)
                    logger.LogError(
                        "Failed to deliver batched ProductCreatedEvent. ProductId: {ProductId}, Reason: {Reason}",
                        evt.ProductId, report.Error.Reason);
            });
            count++;
        }

        // Block until every queued message has been delivered (or errored) before returning, so the
        // caller knows the batch is on the broker.
        _producer.Flush(ct);
        logger.LogInformation("Flushed {Count} batched ProductCreatedEvent(s).", count);
        return Task.CompletedTask;
    }

    public async Task PublishProductUpdatedAsync(ProductUpdatedEvent evt, CancellationToken ct = default)
    {
        var policy = ResiliencePolicies.KafkaRetryPolicy(logger);
        await policy.ExecuteAsync(async () =>
        {
            var message = new Message<string, string>
            {
                Key = evt.ProductId.ToString(),
                Value = JsonSerializer.Serialize(evt),
                Headers = new Headers
                {
                    { "event-type", Encoding.UTF8.GetBytes("ProductUpdatedEvent") },
                    { "version", Encoding.UTF8.GetBytes(evt.Version.ToString()) },
                    { "correlation-id", Encoding.UTF8.GetBytes(evt.CorrelationId) }
                }
            };

            await _producer.ProduceAsync("product-updated", message, ct);
            logger.LogInformation(
                "Published ProductUpdatedEvent. ProductId: {ProductId}",
                evt.ProductId);
        });
    }

    public void Dispose() => _producer.Dispose();
}
