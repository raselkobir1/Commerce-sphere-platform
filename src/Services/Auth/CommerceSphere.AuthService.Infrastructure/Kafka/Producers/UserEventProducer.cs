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

public class UserEventProducer(IConfiguration config, ILogger<UserEventProducer> logger) : IUserEventProducer, IDisposable
{
    private readonly IProducer<string, string> _producer = new ProducerBuilder<string, string>(
        new ProducerConfig { BootstrapServers = config["Kafka:BootstrapServers"] ?? "localhost:9092" }).Build();

    public async Task PublishUserCreatedAsync(UserCreatedEvent evt, CancellationToken ct = default)
    {
        var policy = ResiliencePolicies.KafkaRetryPolicy(logger);
        await policy.ExecuteAsync(async () =>
        {
            var message = new Message<string, string>
            {
                Key = evt.UserId.ToString(),
                Value = JsonSerializer.Serialize(evt),
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
