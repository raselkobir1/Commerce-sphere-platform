using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CommerceSphere.CartService.API.Realtime;

// Admin-only SignalR hub. Connected clients receive an "orderPlaced" message whenever a new
// order is checked out. No server methods are needed — it's a one-way server→admin push.
[Authorize(Roles = "Admin")]
public class OrderNotificationHub : Hub
{
}
