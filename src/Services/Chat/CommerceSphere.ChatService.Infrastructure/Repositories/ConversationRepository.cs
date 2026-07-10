using CommerceSphere.ChatService.Domain.Entities;
using CommerceSphere.ChatService.Domain.Interfaces.Repositories;
using CommerceSphere.ChatService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CommerceSphere.ChatService.Infrastructure.Repositories;

public class ConversationRepository(ChatDbContext db) : IConversationRepository
{
    public Task<Conversation?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Conversations.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Conversation?> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default) =>
        db.Conversations.FirstOrDefaultAsync(c => c.CustomerId == customerId, ct);

    public async Task<IReadOnlyList<Conversation>> GetAllOrderedAsync(CancellationToken ct = default) =>
        await db.Conversations
            .OrderByDescending(c => c.LastMessageAt)
            .ToListAsync(ct);

    public async Task AddAsync(Conversation conversation, CancellationToken ct = default) =>
        await db.Conversations.AddAsync(conversation, ct);

    public void Update(Conversation conversation) =>
        db.Conversations.Update(conversation);
}
