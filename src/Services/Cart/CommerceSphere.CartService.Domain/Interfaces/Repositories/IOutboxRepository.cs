using CommerceSphere.CartService.Domain.Entities;

namespace CommerceSphere.CartService.Domain.Interfaces.Repositories;

public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage message, CancellationToken ct = default);
    // Oldest unpublished messages first (tracked, so the relay can mark them processed).
    Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(int take, CancellationToken ct = default);
}
