using CommerceSphere.NotificationService.Domain.Entities;
using CommerceSphere.NotificationService.Domain.Interfaces.Repositories;
using CommerceSphere.NotificationService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CommerceSphere.NotificationService.Infrastructure.Repositories;

public class InboxRepository(NotificationDbContext db) : IInboxRepository
{
    public Task<bool> ExistsAsync(string key, CancellationToken ct = default) =>
        db.InboxMessages.AnyAsync(m => m.Key == key, ct);

    public async Task AddAsync(InboxMessage message, CancellationToken ct = default) =>
        await db.InboxMessages.AddAsync(message, ct);
}
