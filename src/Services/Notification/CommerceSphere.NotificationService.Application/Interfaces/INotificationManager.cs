using CommerceSphere.NotificationService.Application.DTOs.Responses;

namespace CommerceSphere.NotificationService.Application.Interfaces;

// Drives the admin REST feed (the bell in the header).
public interface INotificationManager
{
    Task<NotificationListResponse> GetAllAsync(CancellationToken ct = default);
    Task MarkAllReadAsync(CancellationToken ct = default);
}
