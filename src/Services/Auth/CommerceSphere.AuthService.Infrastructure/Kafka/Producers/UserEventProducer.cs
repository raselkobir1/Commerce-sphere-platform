using System.Text.Json;
using CommerceSphere.Shared.Common.Resilience;
using CommerceSphere.Shared.Contracts.Events.Auth;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CommerceSphere.AuthService.Infrastructure.Kafka.Producers;

public interface IUserEventProducer
{
    Task PublishUserCreatedAsync(UserCreatedEvent evt, CancellationToken ct = default);
}

// Registered as Singleton so the underlying Kafka producer (which is expensive to create
// and maintains an internal connection pool) is shared across all requests.
public class UserEventProducer(IConfiguration config, ILogger<UserEventProducer> logger) : IUserEventProducer, IDisposable
{
    private readonly IProducer<string, string> _producer = new ProducerBuilder<string, string>(
        new ProducerConfig { BootstrapServers = config["Kafka:BootstrapServers"] ?? "localhost:9092" }).Build();

    public async Task PublishUserCreatedAsync(UserCreatedEvent evt, CancellationToken ct = default)
    {
        // Wrap in the Kafka retry policy (5 attempts with exponential back-off) so transient
        // broker unavailability doesn't drop the event on the floor.
        var policy = ResiliencePolicies.KafkaRetryPolicy(logger);
        await policy.ExecuteAsync(async () =>
        {
            var message = new Message<string, string>
            {
                // Using UserId as the Kafka message key ensures all events for the same user
                // land on the same partition, preserving ordering for that user.
                Key = evt.UserId.ToString(),
                Value = JsonSerializer.Serialize(evt),
                // Headers carry metadata that consumers can read without deserialising the payload —
                // useful for routing, schema evolution (version), and distributed tracing (correlation-id).
                Headers = new Headers
                {
                    { "event-type", System.Text.Encoding.UTF8.GetBytes("UserCreatedEvent") },
                    { "version", System.Text.Encoding.UTF8.GetBytes(evt.Version.ToString()) },
                    { "correlation-id", System.Text.Encoding.UTF8.GetBytes(evt.CorrelationId) }
                }
            };
            await _producer.ProduceAsync("user-created", message, ct);
            logger.LogInformation("Published UserCreatedEvent. UserId: {UserId}", evt.UserId);
        });
    }

    public void Dispose() => _producer.Dispose();
}
