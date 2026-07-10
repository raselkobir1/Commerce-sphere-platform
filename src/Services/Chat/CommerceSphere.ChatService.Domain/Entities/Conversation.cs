namespace CommerceSphere.ChatService.Domain.Entities;

// A support conversation between one customer and the shop's support agents.
// There is a single conversation per customer (their support thread), created on first contact.
public class Conversation : BaseEntity
{
    public Guid CustomerId { get; private set; }

    // Denormalised customer identity so the agent inbox can show who is chatting without a cross-service call.
    public string CustomerName { get; private set; } = string.Empty;
    public string CustomerEmail { get; private set; } = string.Empty;

    // When the most recent message arrived and a short preview of it — drives the inbox ordering/preview.
    public DateTime LastMessageAt { get; private set; } = DateTime.UtcNow;
    public string LastMessagePreview { get; private set; } = string.Empty;

    // Count of customer messages the support side has not opened yet (the inbox "unread" badge).
    public int UnreadForSupport { get; private set; }

    private Conversation() { }

    public static Conversation Start(Guid customerId, string customerName, string customerEmail) =>
        new()
        {
            CustomerId = customerId,
            CustomerName = string.IsNullOrWhiteSpace(customerName) ? "Customer" : customerName,
            CustomerEmail = customerEmail
        };

    // Called whenever a message is added to this conversation.
    // Customer messages bump the unread badge; support replies clear it (the agent is reading the thread).
    public void RecordMessage(string preview, bool fromCustomer)
    {
        LastMessageAt = DateTime.UtcNow;
        LastMessagePreview = Preview(preview);
        if (fromCustomer) UnreadForSupport++;
        else UnreadForSupport = 0;
        SetUpdated();
    }

    // Agent opened the thread — clear the unread badge.
    public void MarkReadBySupport()
    {
        if (UnreadForSupport == 0) return;
        UnreadForSupport = 0;
        SetUpdated();
    }

    private static string Preview(string text) =>
        text.Length <= 120 ? text : text[..117] + "...";
}
