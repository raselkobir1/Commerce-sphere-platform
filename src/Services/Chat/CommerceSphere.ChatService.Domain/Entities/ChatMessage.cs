namespace CommerceSphere.ChatService.Domain.Entities;

// A single message within a conversation, sent by either the customer or a support agent.
// Messages are immutable once created and persisted so history is a durable audit trail.
public class ChatMessage : BaseEntity
{
    public Guid ConversationId { get; private set; }

    public Guid SenderId { get; private set; }
    public string SenderRole { get; private set; } = Entities.SenderRole.Customer;  // "Customer" | "Support"
    public string SenderName { get; private set; } = string.Empty;

    public string Content { get; private set; } = string.Empty;

    private ChatMessage() { }

    public static ChatMessage Create(Guid conversationId, Guid senderId, string senderRole, string senderName, string content) =>
        new()
        {
            ConversationId = conversationId,
            SenderId = senderId,
            SenderRole = senderRole,
            SenderName = string.IsNullOrWhiteSpace(senderName) ? "Unknown" : senderName,
            Content = content.Trim()
        };
}
