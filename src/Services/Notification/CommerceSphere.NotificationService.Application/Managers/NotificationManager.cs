using CommerceSphere.NotificationService.Application.DTOs.Responses;
using CommerceSphere.NotificationService.Application.Interfaces;
using CommerceSphere.NotificationService.Domain.Entities;
using CommerceSphere.NotificationService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CommerceSphere.NotificationService.Application.Managers;

public class NotificationManager(IUnitOfWork uow, ILogger<NotificationManager> logger) : INotificationManager
{
    private const int RecentLimit = 30;

    public async Task<NotificationListResponse> GetAllAsync(CancellationToken ct = default)
    {
        var items = await uow.Notifications.GetRecentAsync(RecentLimit, ct);
        var unread = await uow.Notifications.CountUnreadAsync(ct);
        return new NotificationListResponse(items.Select(Map).ToList(), unread);
    }

    public async Task MarkAllReadAsync(CancellationToken ct = default)
    {
        var updated = await uow.Notifications.MarkAllReadAsync(ct);
        if (updated > 0) logger.LogInformation("Marked {Count} notification(s) read.", updated);
    }

    internal static NotificationResponse Map(Notification n) =>
        new(n.Id, n.Type, n.Title, n.Message, n.OrderId, n.UserId, n.Amount, n.ItemCount, n.IsRead, n.CreatedAt);
}
