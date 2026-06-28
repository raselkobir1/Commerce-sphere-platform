using CommerceSphere.CartService.Application.Interfaces;
using CommerceSphere.Shared.Common.Correlation;
using CommerceSphere.Shared.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommerceSphere.CartService.API.Controllers;

// Admin notification feed. Routed under /api/carts so it reuses the existing gateway route.
[ApiController]
[Route("api/carts/notifications")]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
public class NotificationController(INotificationManager notificationManager) : ControllerBase
{
    // Recent notifications + the current unread count (used to seed the badge on page load).
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotifications(CancellationToken ct)
    {
        var result = await notificationManager.GetAllAsync(ct);
        return Ok(ApiResponse<object>.Ok(result, "Notifications retrieved", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    // Marks every notification as read → unread count becomes 0 (called when the admin opens the panel).
    [HttpPost("read")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await notificationManager.MarkAllReadAsync(ct);
        return Ok(ApiResponse.Ok("Notifications marked read", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }
}
