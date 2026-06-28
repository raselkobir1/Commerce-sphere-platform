using System.Text;
using CommerceSphere.CartService.Domain.Interfaces;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CommerceSphere.CartService.Infrastructure.Kafka;

// Publishes pending outbox rows to Kafka, then marks them processed. Runs continuously, so if
// Kafka is down when an order is placed the row simply waits and is published once Kafka is back —
// the event is never lost. At-least-once delivery; consumers dedupe by business key.
public class OutboxRelay(
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    ILogger<OutboxRelay> logger) : BackgroundService
{
    private const int BatchSize = 20;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = config["Kafka:BootstrapServers"] ?? "localhost:9092",
            EnableIdempotence = true,
            Acks = Acks.All,
        };
        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();
        logger.LogInformation("Cart OutboxRelay started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishPendingAsync(producer, stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "OutboxRelay loop error.");
            }
            await Task.Delay(PollInterval, stoppingToken);
        }

        logger.LogInformation("Cart OutboxRelay stopped.");
    }

    private async Task PublishPendingAsync(IProducer<string, string> producer, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var pending = await uow.Outbox.GetUnprocessedAsync(BatchSize, ct);
        if (pending.Count == 0) return;

        foreach (var msg in pending)
        {
            try
            {
                var kafkaMessage = new Message<string, string>
                {
                    Key = msg.Key,
                    Value = msg.Payload,
                    Headers =
                    [
                        new Header("event-type", Encoding.UTF8.GetBytes(msg.EventType)),
                        new Header("correlation-id", Encoding.UTF8.GetBytes(msg.CorrelationId)),
                    ],
                };
                await producer.ProduceAsync(msg.Topic, kafkaMessage, ct);
                msg.MarkProcessed();
                logger.LogInformation("Outbox published {EventType} to {Topic}.", msg.EventType, msg.Topic);
            }
            catch (Exception ex)
            {
                msg.RecordFailedAttempt();
                logger.LogWarning(ex, "Outbox publish failed for {Id} (attempt {Attempts}); will retry.", msg.Id, msg.Attempts);
            }
        }

        await uow.SaveChangesAsync(ct);
    }
}
