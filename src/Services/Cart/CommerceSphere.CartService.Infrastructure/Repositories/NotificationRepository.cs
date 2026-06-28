using CommerceSphere.CartService.Domain.Entities;
using CommerceSphere.CartService.Domain.Interfaces.Repositories;
using CommerceSphere.CartService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CommerceSphere.CartService.Infrastructure.Repositories;

public class NotificationRepository(CartDbContext context) : INotificationRepository
{
    public async Task AddAsync(Notification notification, CancellationToken ct = default) =>
        await context.Notifications.AddAsync(notification, ct);

    public async Task<IReadOnlyList<Notification>> GetRecentAsync(int take, CancellationToken ct = default) =>
        await context.Notifications
            .AsNoTracking()
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .ToListAsync(ct);

    public Task<int> CountUnreadAsync(CancellationToken ct = default) =>
        context.Notifications.CountAsync(n => !n.IsRead, ct);

    public async Task<int> MarkAllReadAsync(CancellationToken ct = default)
    {
        // Bulk update so marking many notifications read is a single round-trip.
        return await context.Notifications
            .Where(n => !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.UpdatedAt, DateTime.UtcNow), ct);
    }
}
