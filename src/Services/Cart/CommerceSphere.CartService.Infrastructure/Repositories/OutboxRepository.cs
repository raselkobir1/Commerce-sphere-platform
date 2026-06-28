using CommerceSphere.CartService.Domain.Entities;
using CommerceSphere.CartService.Domain.Interfaces.Repositories;
using CommerceSphere.CartService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CommerceSphere.CartService.Infrastructure.Repositories;

public class OutboxRepository(CartDbContext context) : IOutboxRepository
{
    public async Task AddAsync(OutboxMessage message, CancellationToken ct = default) =>
        await context.OutboxMessages.AddAsync(message, ct);

    public async Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(int take, CancellationToken ct = default) =>
        await context.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(take)
            .ToListAsync(ct);
}
