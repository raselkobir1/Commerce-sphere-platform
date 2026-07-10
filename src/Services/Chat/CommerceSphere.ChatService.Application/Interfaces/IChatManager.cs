using CommerceSphere.ChatService.Application.DTOs.Requests;
using CommerceSphere.ChatService.Application.DTOs.Responses;

namespace CommerceSphere.ChatService.Application.Interfaces;

public interface IChatManager
{
    // Returns the caller's support conversation, creating it on first contact. Customer-facing.
    Task<ConversationResponse> GetOrCreateMyConversationAsync(ChatUser customer, CancellationToken ct = default);

    // Agent inbox: every conversation, newest activity first.
    Task<IReadOnlyList<ConversationResponse>> GetConversationsAsync(CancellationToken ct = default);

    // Message history for a conversation. Customers may only read their own; agents may read any.
    // When an agent opens a thread, the unread badge is cleared.
    Task<IReadOnlyList<ChatMessageResponse>> GetMessagesAsync(Guid conversationId, ChatUser caller, CancellationToken ct = default);

    // Persists a message from the caller into the conversation, then pushes it in real time.
    // Customers may only post to their own conversation; agents may post to any.
    Task<ChatMessageResponse> SendMessageAsync(Guid conversationId, ChatUser caller, SendMessageRequest request, CancellationToken ct = default);
}
