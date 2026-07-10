namespace CommerceSphere.ChatService.Application.DTOs.Responses;

// A conversation as shown to clients — to the customer (their own thread) and to the agent inbox.
public record ConversationResponse(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    string CustomerEmail,
    string LastMessagePreview,
    DateTime LastMessageAt,
    int UnreadForSupport
);

// A single chat message.
public record ChatMessageResponse(
    Guid Id,
    Guid ConversationId,
    Guid SenderId,
    string SenderRole,   // "Customer" | "Support"
    string SenderName,
    string Content,
    DateTime SentAt
);
