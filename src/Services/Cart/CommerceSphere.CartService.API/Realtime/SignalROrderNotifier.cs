using CommerceSphere.CartService.Application.DTOs.Responses;
using CommerceSphere.CartService.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace CommerceSphere.CartService.API.Realtime;

// Broadcasts notifications to every connected admin via the OrderNotificationHub.
public class SignalROrderNotifier(IHubContext<OrderNotificationHub> hub) : IOrderNotifier
{
    public Task OrderPlacedAsync(NotificationResponse notification, CancellationToken ct = default) =>
        hub.Clients.All.SendAsync("orderPlaced", notification, ct);
}
