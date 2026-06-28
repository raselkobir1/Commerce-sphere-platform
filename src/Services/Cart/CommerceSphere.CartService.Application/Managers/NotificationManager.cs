using CommerceSphere.CartService.Application.DTOs.Responses;
using CommerceSphere.CartService.Application.Interfaces;
using CommerceSphere.CartService.Domain.Entities;
using CommerceSphere.CartService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CommerceSphere.CartService.Application.Managers;

public class NotificationManager(
    IUnitOfWork uow,
    IOrderNotifier notifier,
    ILogger<NotificationManager> logger) : INotificationManager
{
    private const int RecentLimit = 30;

    public async Task<NotificationListResponse> GetAllAsync(CancellationToken ct = default)
    {
        var items = await uow.Notifications.GetRecentAsync(RecentLimit, ct);
        var unread = await uow.Notifications.CountUnreadAsync(ct);
        return new NotificationListResponse(items.Select(MapToResponse).ToList(), unread);
    }

    public async Task MarkAllReadAsync(CancellationToken ct = default)
    {
        var updated = await uow.Notifications.MarkAllReadAsync(ct);
        if (updated > 0)
            logger.LogInformation("Marked {Count} notification(s) as read.", updated);
    }

    public async Task CreateOrderPlacedAsync(Cart order, CancellationToken ct = default)
    {
        var notification = Notification.OrderPlaced(order.Id, order.UserId, order.TotalAmount, order.ItemCount);
        await uow.Notifications.AddAsync(notification, ct);
        await uow.SaveChangesAsync(ct);

        // Best-effort live push — never let a transport hiccup fail the checkout.
        try
        {
            await notifier.OrderPlacedAsync(MapToResponse(notification), ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to push order-placed notification {NotificationId} to admins.", notification.Id);
        }

        logger.LogInformation("Order-placed notification created. OrderId: {OrderId}", order.Id);
    }

    private static NotificationResponse MapToResponse(Notification n) =>
        new(n.Id, n.Type, n.Title, n.Message, n.OrderId, n.UserId, n.Amount, n.ItemCount, n.IsRead, n.CreatedAt);
}
