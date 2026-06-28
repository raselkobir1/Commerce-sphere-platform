using CommerceSphere.NotificationService.Application.DTOs.Responses;

namespace CommerceSphere.NotificationService.Application.Interfaces;

// Pushes a notification to connected admin clients in real time. Implemented in the API layer
// over SignalR; abstracted here so the Application layer stays transport-agnostic.
public interface IRealtimeNotifier
{
    Task NotificationCreatedAsync(NotificationResponse notification, CancellationToken ct = default);
}
