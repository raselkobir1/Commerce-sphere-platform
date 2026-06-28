namespace CommerceSphere.NotificationService.Domain.Entities;

// One row per event we've already handled. The Key is a business key (e.g. "checkedout:{cartId}"),
// so a redelivered Kafka message is recognised and skipped — this is what prevents double-notifying.
public class InboxMessage
{
    public string Key { get; private set; } = string.Empty;
    public DateTime ProcessedAt { get; private set; } = DateTime.UtcNow;

    private InboxMessage() { }

    public static InboxMessage For(string key) => new() { Key = key };
}
