using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CommerceSphere.NotificationService.API.Realtime;

// Admin-only SignalR hub. Connected clients receive a "notification" message whenever a new
// admin notification is created. One-way server→admin push; no client-callable methods.
[Authorize(Roles = "Admin")]
public class NotificationHub : Hub
{
}
