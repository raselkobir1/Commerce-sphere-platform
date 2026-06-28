using CommerceSphere.NotificationService.Domain.Entities;

namespace CommerceSphere.NotificationService.Domain.Interfaces.Repositories;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken ct = default);
    Task<IReadOnlyList<Notification>> GetRecentAsync(int take, CancellationToken ct = default);
    Task<int> CountUnreadAsync(CancellationToken ct = default);
    Task<int> MarkReadAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default);
    Task<int> MarkAllReadAsync(CancellationToken ct = default);
}
