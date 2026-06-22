using System.Text;
using System.Text.Json;
using CommerceSphere.InventoryService.Application.Interfaces;
using CommerceSphere.Shared.Common.Resilience;
using CommerceSphere.Shared.Contracts.Events.Inventory;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CommerceSphere.InventoryService.Infrastructure.Kafka.Producers;

public class InventoryEventProducer(IConfiguration config, ILogger<InventoryEventProducer> logger)
    : IInventoryEventProducer, IDisposable
{
    private readonly IProducer<string, string> _producer = new ProducerBuilder<string, string>(
        new ProducerConfig
        {
            BootstrapServers = config["Kafka:BootstrapServers"] ?? "localhost:9092"
        }).Build();

    public async Task PublishReservedAsync(InventoryReservedEvent evt, CancellationToken ct = default)
    {
        var policy = ResiliencePolicies.KafkaRetryPolicy(logger);
        await policy.ExecuteAsync(async () =>
        {
            var message = new Message<string, string>
            {
                Key = evt.ReservationId.ToString(),
                Value = JsonSerializer.Serialize(evt),
                Headers = new Headers
                {
                    { "event-type", Encoding.UTF8.GetBytes("InventoryReservedEvent") },
                    { "version", Encoding.UTF8.GetBytes(evt.Version.ToString()) },
                    { "correlation-id", Encoding.UTF8.GetBytes(evt.CorrelationId) }
                }
            };

            await _producer.ProduceAsync("inventory-reserved", message, ct);
            logger.LogInformation(
                "Published InventoryReservedEvent. ReservationId: {ReservationId}, CartId: {CartId}",
                evt.ReservationId, evt.CartId);
        });
    }

    public async Task PublishReservationFailedAsync(InventoryReservationFailedEvent evt, CancellationToken ct = default)
    {
        var policy = ResiliencePolicies.KafkaRetryPolicy(logger);
        await policy.ExecuteAsync(async () =>
        {
            var message = new Message<string, string>
            {
                Key = evt.CartId.ToString(),
                Value = JsonSerializer.Serialize(evt),
                Headers = new Headers
                {
                    { "event-type", Encoding.UTF8.GetBytes("InventoryReservationFailedEvent") },
                    { "version", Encoding.UTF8.GetBytes(evt.Version.ToString()) },
                    { "correlation-id", Encoding.UTF8.GetBytes(evt.CorrelationId) }
                }
            };

            await _producer.ProduceAsync("inventory-reservation-failed", message, ct);
            logger.LogInformation(
                "Published InventoryReservationFailedEvent. CartId: {CartId}, Reason: {Reason}",
                evt.CartId, evt.Reason);
        });
    }

    public async Task PublishReleasedAsync(InventoryReleasedEvent evt, CancellationToken ct = default)
    {
        var policy = ResiliencePolicies.KafkaRetryPolicy(logger);
        await policy.ExecuteAsync(async () =>
        {
            var message = new Message<string, string>
            {
                Key = evt.ReservationId.ToString(),
                Value = JsonSerializer.Serialize(evt),
                Headers = new Headers
                {
                    { "event-type", Encoding.UTF8.GetBytes("InventoryReleasedEvent") },
                    { "version", Encoding.UTF8.GetBytes(evt.Version.ToString()) },
                    { "correlation-id", Encoding.UTF8.GetBytes(evt.CorrelationId) }
                }
            };

            await _producer.ProduceAsync("inventory-released", message, ct);
            logger.LogInformation(
                "Published InventoryReleasedEvent. ReservationId: {ReservationId}, CartId: {CartId}",
                evt.ReservationId, evt.CartId);
        });
    }

    public void Dispose() => _producer.Dispose();
}
