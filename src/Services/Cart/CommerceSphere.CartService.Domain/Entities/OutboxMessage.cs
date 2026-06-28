namespace CommerceSphere.CartService.Domain.Entities;

// Transactional outbox row. The event is written here in the SAME database transaction as the
// cart change, so it can never be lost — a background relay publishes it to Kafka afterwards and
// marks it processed. If Kafka is down at checkout time, the row simply waits and is retried.
public class OutboxMessage
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Topic { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;   // JSON of the event
    public string EventType { get; private set; } = string.Empty;
    public string CorrelationId { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; private set; }            // null = still pending
    public int Attempts { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage Create(string topic, string key, string payload, string eventType, string correlationId) =>
        new()
        {
            Topic = topic,
            Key = key,
            Payload = payload,
            EventType = eventType,
            CorrelationId = correlationId,
        };

    public void MarkProcessed() => ProcessedAt = DateTime.UtcNow;
    public void RecordFailedAttempt() => Attempts++;
}
