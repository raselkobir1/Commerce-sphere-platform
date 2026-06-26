using CommerceSphere.AuthService.Application.DTOs.Requests;
using CommerceSphere.AuthService.Application.Interfaces;
using CommerceSphere.Shared.Common.Correlation;
using CommerceSphere.Shared.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommerceSphere.AuthService.API.Controllers;

[ApiController]
[Route("api/auth/email")]
[Produces("application/json")]
public class EmailVerificationController(IAccountManager accountManager) : ControllerBase
{
    [HttpPost("verify/send")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SendVerification(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value ?? throw new UnauthorizedAccessException());
        await accountManager.SendVerificationEmailAsync(userId, ct);
        return Ok(ApiResponse.Ok("Verification email sent", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    [HttpPost("verify/resend")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationEmailRequest request, CancellationToken ct)
    {
        await accountManager.ResendVerificationEmailAsync(request, ct);
        return Ok(ApiResponse.Ok("If that email exists and is unverified, a new link has been sent.", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    [HttpGet("verify/confirm")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmVerification([FromQuery] string token, CancellationToken ct)
    {
        await accountManager.VerifyEmailAsync(new VerifyEmailRequest(token), ct);
        return Ok(ApiResponse.Ok("Email verified successfully", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }
}
