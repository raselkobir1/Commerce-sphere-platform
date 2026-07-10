using CommerceSphere.ChatService.Domain.Entities;

namespace CommerceSphere.ChatService.Domain.Interfaces.Repositories;

public interface IChatMessageRepository
{
    // Full message history for a conversation, oldest first (chat reading order).
    Task<IReadOnlyList<ChatMessage>> GetByConversationAsync(Guid conversationId, CancellationToken ct = default);

    Task AddAsync(ChatMessage message, CancellationToken ct = default);
}
