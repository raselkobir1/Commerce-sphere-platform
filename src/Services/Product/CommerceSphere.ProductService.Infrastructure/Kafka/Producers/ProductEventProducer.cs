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
