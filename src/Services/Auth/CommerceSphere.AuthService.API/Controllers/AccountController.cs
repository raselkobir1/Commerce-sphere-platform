using CommerceSphere.AuthService.Application.DTOs.Requests;
using CommerceSphere.AuthService.Application.Interfaces;
using CommerceSphere.Shared.Common.Correlation;
using CommerceSphere.Shared.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommerceSphere.AuthService.API.Controllers;

[ApiController]
[Route("api/auth")]
[Authorize]
[Produces("application/json")]
public class AccountController(IAccountManager accountManager) : ControllerBase
{
    [HttpPatch("me")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var result = await accountManager.UpdateProfileAsync(GetUserId(), request, ct);
        return Ok(ApiResponse<object>.Ok(result, "Profile updated", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    [HttpPost("change-password")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        await accountManager.ChangePasswordAsync(GetUserId(), request, ct);
        return Ok(ApiResponse.Ok("Password changed successfully. All other sessions have been revoked.", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    [HttpGet("sessions")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessions(CancellationToken ct)
    {
        var sessions = await accountManager.GetActiveSessionsAsync(GetUserId(), ct);
        return Ok(ApiResponse<object>.Ok(sessions, "Sessions retrieved", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    [HttpDelete("sessions")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RevokeAllSessions(CancellationToken ct)
    {
        await accountManager.RevokeAllSessionsAsync(GetUserId(), ct);
        return Ok(ApiResponse.Ok("All sessions revoked", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value ?? throw new UnauthorizedAccessException());
}
