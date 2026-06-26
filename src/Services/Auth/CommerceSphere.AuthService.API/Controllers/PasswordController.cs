using CommerceSphere.AuthService.Application.DTOs.Requests;
using CommerceSphere.AuthService.Application.Interfaces;
using CommerceSphere.Shared.Common.Correlation;
using CommerceSphere.Shared.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace CommerceSphere.AuthService.API.Controllers;

[ApiController]
[Route("api/auth/password")]
[Produces("application/json")]
public class PasswordController(IAccountManager accountManager) : ControllerBase
{
    [HttpPost("forgot")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        await accountManager.ForgotPasswordAsync(request, ct);
        return Ok(ApiResponse.Ok("If that email is registered, a reset link has been sent.", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    [HttpPost("reset")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        await accountManager.ResetPasswordAsync(request, ct);
        return Ok(ApiResponse.Ok("Password reset successfully. Please log in with your new password.", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }
}
