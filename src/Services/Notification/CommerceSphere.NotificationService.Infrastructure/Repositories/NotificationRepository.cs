using CommerceSphere.NotificationService.Domain.Entities;
using CommerceSphere.NotificationService.Domain.Interfaces.Repositories;
using CommerceSphere.NotificationService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CommerceSphere.NotificationService.Infrastructure.Repositories;

public class NotificationRepository(NotificationDbContext db) : INotificationRepository
{
    public async Task AddAsync(Notification notification, CancellationToken ct = default) =>
        await db.Notifications.AddAsync(notification, ct);

    public async Task<IReadOnlyList<Notification>> GetRecentAsync(int take, CancellationToken ct = default) =>
        await db.Notifications.AsNoTracking()
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .ToListAsync(ct);

    public Task<int> CountUnreadAsync(CancellationToken ct = default) =>
        db.Notifications.CountAsync(n => !n.IsRead, ct);

    public async Task<int> MarkReadAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0) return 0;
        return await db.Notifications
            .Where(n => !n.IsRead && ids.Contains(n.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.UpdatedAt, DateTime.UtcNow), ct);
    }

    public async Task<int> MarkAllReadAsync(CancellationToken ct = default) =>
        await db.Notifications
            .Where(n => !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.UpdatedAt, DateTime.UtcNow), ct);
}
