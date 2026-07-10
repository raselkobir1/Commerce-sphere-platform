using CommerceSphere.ChatService.Domain.Entities;
using CommerceSphere.ChatService.Domain.Interfaces.Repositories;
using CommerceSphere.ChatService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CommerceSphere.ChatService.Infrastructure.Repositories;

public class ChatMessageRepository(ChatDbContext db) : IChatMessageRepository
{
    public async Task<IReadOnlyList<ChatMessage>> GetByConversationAsync(Guid conversationId, CancellationToken ct = default) =>
        await db.Messages
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(ChatMessage message, CancellationToken ct = default) =>
        await db.Messages.AddAsync(message, ct);
}
