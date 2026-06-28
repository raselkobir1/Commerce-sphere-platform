using CommerceSphere.CartService.Application.DTOs.Responses;
using CommerceSphere.CartService.Domain.Entities;

namespace CommerceSphere.CartService.Application.Interfaces;

public interface INotificationManager
{
    Task<NotificationListResponse> GetAllAsync(CancellationToken ct = default);
    Task MarkAllReadAsync(CancellationToken ct = default);
    // Persists an "order placed" notification and pushes it live to admins. Best-effort push:
    // a SignalR failure never breaks the checkout that triggered it.
    Task CreateOrderPlacedAsync(Cart order, CancellationToken ct = default);
}
