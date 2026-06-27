using System.Text.Json;
using CommerceSphere.CartService.Application.Interfaces;
using CommerceSphere.Shared.Common.Resilience;
using CommerceSphere.Shared.Contracts.Events.Cart;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CommerceSphere.CartService.Infrastructure.Kafka.Producers;

public class CartEventProducer : ICartEventProducer, IDisposable
{
    private const string CartCreatedTopic = "cart-created";
    private const string CartUpdatedTopic = "cart-updated";
    private const string CartCheckedOutTopic = "cart-checkedout";
    private const string CartRolledBackTopic = "cart-rolledback";
    private const string CartCancelledTopic = "cart-cancelled";

    private readonly IProducer<string, string> _producer;
    private readonly ILogger<CartEventProducer> _logger;

    public CartEventProducer(IConfiguration configuration, ILogger<CartEventProducer> logger)
    {
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "kafka:9092",
            EnableIdempotence = true,
            Acks = Acks.All,
            MessageSendMaxRetries = 3,
            RetryBackoffMs = 1000
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishCartCreatedAsync(CartCreatedEvent @event)
    {
        await PublishAsync(CartCreatedTopic, @event.CartId.ToString(), @event, "CartCreated", @event.CorrelationId);
    }

    public async Task PublishCartUpdatedAsync(CartUpdatedEvent @event)
    {
        await PublishAsync(CartUpdatedTopic, @event.CartId.ToString(), @event, "CartUpdated", @event.CorrelationId);
    }

    public async Task PublishCartCheckedOutAsync(CartCheckedOutEvent @event)
    {
        await PublishAsync(CartCheckedOutTopic, @event.CartId.ToString(), @event, "CartCheckedOut", @event.CorrelationId);
    }

    public async Task PublishCartRolledBackAsync(CartRolledBackEvent @event)
    {
        await PublishAsync(CartRolledBackTopic, @event.CartId.ToString(), @event, "CartRolledBack", @event.CorrelationId);
    }

    public async Task PublishCartCancelledAsync(CartCancelledEvent @event)
    {
        await PublishAsync(CartCancelledTopic, @event.CartId.ToString(), @event, "CartCancelled", @event.CorrelationId);
    }

    private async Task PublishAsync<T>(string topic, string key, T @event, string eventType, string correlationId)
    {
        var policy = ResiliencePolicies.KafkaRetryPolicy(_logger);

        await policy.ExecuteAsync(async () =>
        {
            var message = new Message<string, string>
            {
                Key = key,
                Value = JsonSerializer.Serialize(@event),
                Headers = new Headers
                {
                    { "event-type", System.Text.Encoding.UTF8.GetBytes(eventType) },
                    { "version", System.Text.Encoding.UTF8.GetBytes("1") },
                    { "correlation-id", System.Text.Encoding.UTF8.GetBytes(correlationId ?? string.Empty) }
                }
            };

            var result = await _producer.ProduceAsync(topic, message);
            _logger.LogInformation(
                "Published {EventType} to topic {Topic} partition {Partition} offset {Offset}",
                eventType, topic, result.Partition, result.Offset);
        });
    }

    public void Dispose()
    {
        _producer?.Flush(TimeSpan.FromSeconds(10));
        _producer?.Dispose();
    }
}
