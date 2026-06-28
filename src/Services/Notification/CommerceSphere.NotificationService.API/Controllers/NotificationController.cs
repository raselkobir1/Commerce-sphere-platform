using CommerceSphere.NotificationService.Application.Interfaces;
using CommerceSphere.Shared.Common.Correlation;
using CommerceSphere.Shared.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommerceSphere.NotificationService.API.Controllers;

// Admin notification feed (the bell in the AdminSphere header).
[ApiController]
[Route("api/notifications")]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
public class NotificationController(INotificationManager manager) : ControllerBase
{
    // Recent notifications + the current unread count (seeds the badge on page load).
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await manager.GetAllAsync(ct);
        return Ok(ApiResponse<object>.Ok(result, "Notifications retrieved", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    // Marks every notification read → unread count becomes 0 (called when the admin opens the panel).
    [HttpPost("read")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await manager.MarkAllReadAsync(ct);
        return Ok(ApiResponse.Ok("Notifications marked read", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }
}
