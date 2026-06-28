using CommerceSphere.CartService.Application.DTOs.Responses;

namespace CommerceSphere.CartService.Application.Interfaces;

// Pushes a notification to connected admin clients in real time. Implemented in the API layer
// over SignalR; abstracted here so the Application layer stays transport-agnostic.
public interface IOrderNotifier
{
    Task OrderPlacedAsync(NotificationResponse notification, CancellationToken ct = default);
}
