using CommerceSphere.AuthService.Application.DTOs.Requests;
using CommerceSphere.AuthService.Application.Interfaces;
using CommerceSphere.Shared.Common.Correlation;
using CommerceSphere.Shared.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommerceSphere.AuthService.API.Controllers;

[ApiController]
[Route("api/auth/2fa")]
[Produces("application/json")]
public class TwoFactorController(ITwoFactorManager twoFactorManager) : ControllerBase
{
    // Step 1: get the secret + QR code URI. Call this while authenticated.
    [HttpPost("setup")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Setup(CancellationToken ct)
    {
        var result = await twoFactorManager.SetupAsync(GetUserId(), ct);
        return Ok(ApiResponse<object>.Ok(result,
            "Scan the QR code with your authenticator app, then confirm with /api/auth/2fa/confirm.",
            HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    // Step 2: confirm setup by providing the first TOTP code. Returns fresh tokens with updated 2FA status.
    [HttpPost("confirm")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmSetup([FromBody] ConfirmTwoFactorRequest request, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var tokens = await twoFactorManager.ConfirmSetupAsync(GetUserId(), request, ip, ct);
        return Ok(ApiResponse<object>.Ok(tokens, "Two-factor authentication is now active.", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    // Disable 2FA. Requires the current TOTP code to prevent accidental/unauthorized disabling.
    [HttpPost("disable")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Disable([FromBody] DisableTwoFactorRequest request, CancellationToken ct)
    {
        await twoFactorManager.DisableAsync(GetUserId(), request, ct);
        return Ok(ApiResponse.Ok("Two-factor authentication has been disabled.", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    // Step 3 (on login): submit the TOTP code for the pending challenge.
    [HttpPost("verify")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Verify([FromBody] TwoFactorChallengeRequest request, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var tokens = await twoFactorManager.VerifyChallengeAsync(request, ip, ct);
        return Ok(ApiResponse<object>.Ok(tokens, "Login successful", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value ?? throw new UnauthorizedAccessException());
}
