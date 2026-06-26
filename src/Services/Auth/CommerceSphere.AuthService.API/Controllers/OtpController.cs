using CommerceSphere.AuthService.Application.DTOs.Requests;
using CommerceSphere.AuthService.Application.Interfaces;
using CommerceSphere.Shared.Common.Correlation;
using CommerceSphere.Shared.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommerceSphere.AuthService.API.Controllers;

[ApiController]
[Route("api/auth/otp")]
[Produces("application/json")]
public class OtpController(IOtpManager otpManager) : ControllerBase
{
    // Submit the OTP code received by email during login.
    [HttpPost("verify")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Verify([FromBody] OtpChallengeRequest request, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var tokens = await otpManager.VerifyChallengeAsync(request, ip, ct);
        return Ok(ApiResponse<object>.Ok(tokens, "Login successful", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    // Enable or disable OTP auth for the authenticated user.
    [HttpPost("toggle")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Toggle([FromBody] ToggleOtpRequest request, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value ?? throw new UnauthorizedAccessException());
        await otpManager.ToggleOtpAuthAsync(userId, request, ct);
        return Ok(ApiResponse.Ok($"OTP authentication {(request.Enable ? "enabled" : "disabled")}.", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }
}
