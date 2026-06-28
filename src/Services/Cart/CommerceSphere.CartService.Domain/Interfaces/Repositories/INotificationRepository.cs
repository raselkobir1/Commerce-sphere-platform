using CommerceSphere.CartService.Domain.Entities;

namespace CommerceSphere.CartService.Domain.Interfaces.Repositories;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken ct = default);
    // Most recent notifications first, capped at take.
    Task<IReadOnlyList<Notification>> GetRecentAsync(int take, CancellationToken ct = default);
    Task<int> CountUnreadAsync(CancellationToken ct = default);
    // Marks every unread notification as read; returns how many were updated.
    Task<int> MarkAllReadAsync(CancellationToken ct = default);
}
