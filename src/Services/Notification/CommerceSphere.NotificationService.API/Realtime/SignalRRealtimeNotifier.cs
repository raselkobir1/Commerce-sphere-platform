using CommerceSphere.NotificationService.Application.DTOs.Responses;
using CommerceSphere.NotificationService.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace CommerceSphere.NotificationService.API.Realtime;

// Broadcasts notifications to every connected admin via the NotificationHub.
public class SignalRRealtimeNotifier(IHubContext<NotificationHub> hub) : IRealtimeNotifier
{
    public Task NotificationCreatedAsync(NotificationResponse notification, CancellationToken ct = default) =>
        hub.Clients.All.SendAsync("notification", notification, ct);
}
