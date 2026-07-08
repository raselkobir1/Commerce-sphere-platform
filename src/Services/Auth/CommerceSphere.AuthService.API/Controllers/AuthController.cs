using CommerceSphere.AuthService.Application.DTOs.Requests;
using CommerceSphere.AuthService.Application.DTOs.Responses;
using CommerceSphere.AuthService.Application.Interfaces;
using CommerceSphere.Shared.Common.Authorization;
using CommerceSphere.Shared.Common.Correlation;
using CommerceSphere.Shared.Common.Exceptions;
using CommerceSphere.Shared.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommerceSphere.AuthService.API.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(IAuthManager authManager) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.GetCorrelationId();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await authManager.RegisterAsync(request, ip, correlationId, ct);
        return CreatedAtAction(nameof(GetMe), null,
            ApiResponse<object>.Ok(result, "Registration successful. Check your email to verify your account.", HttpContext.TraceIdentifier, correlationId));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.GetCorrelationId();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await authManager.LoginAsync(request, ip, correlationId, ct);

        return result switch
        {
            LoginSucceeded s =>
                Ok(ApiResponse<object>.Ok(s.Tokens, "Login successful", HttpContext.TraceIdentifier, correlationId)),

            LoginNeedsTwoFactor t =>
                Ok(ApiResponse<object>.Ok(
                    new { requiresTwoFactor = true, t.ChallengeToken },
                    "Two-factor authentication required. Submit the code to /api/auth/2fa/verify.",
                    HttpContext.TraceIdentifier, correlationId)),

            LoginNeedsOtp o =>
                Ok(ApiResponse<object>.Ok(
                    new { requiresOtp = true, o.ChallengeToken },
                    "A one-time code has been sent to your email. Submit it to /api/auth/otp/verify.",
                    HttpContext.TraceIdentifier, correlationId)),

            LoginNeedsPasswordChange p =>
                Ok(ApiResponse<object>.Ok(
                    new { requiresPasswordChange = true, p.ChallengeToken },
                    "You must set a new password before continuing. Submit it to /api/auth/password/complete-forced-change.",
                    HttpContext.TraceIdentifier, correlationId)),

            _ => throw new InvalidOperationException("Unexpected login result type.")
        };
    }

    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await authManager.RefreshTokenAsync(request, ip, ct);
        return Ok(ApiResponse<object>.Ok(result, "Token refreshed", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    [HttpPost("revoke-token")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequest request, CancellationToken ct)
    {
        await authManager.RevokeTokenAsync(request, ct);
        return Ok(ApiResponse.Ok("Token revoked", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var userId = GetUserId();
        var user = await authManager.GetUserByIdAsync(userId, ct);
        return Ok(ApiResponse<object>.Ok(user, "User retrieved", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    [HttpGet("users")]
    [HasPermission("users:view")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers([FromQuery] PagedRequest paged, CancellationToken ct)
    {
        var result = await authManager.GetUsersAsync(paged, ct);
        return Ok(ApiResponse<object>.Ok(result, "Users retrieved", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    [HttpPost("users")]
    [HasPermission("users:create")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUser([FromBody] AdminCreateUserRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.GetCorrelationId();
        var result = await authManager.AdminCreateUserAsync(request, correlationId, ct);
        return Ok(ApiResponse<object>.Ok(result, "User created", HttpContext.TraceIdentifier, correlationId));
    }

    [HttpPut("users/{id:guid}")]
    [HasPermission("users:edit")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] AdminUpdateUserRequest request, CancellationToken ct)
    {
        var result = await authManager.AdminUpdateUserAsync(id, request, ct);
        return Ok(ApiResponse<object>.Ok(result, "User updated", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    [HttpDelete("users/{id:guid}")]
    [HasPermission("users:delete")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken ct)
    {
        if (id == GetUserId())
            return BadRequest(ApiResponse.Fail("You cannot delete your own account."));
        await authManager.AdminDeleteUserAsync(id, ct);
        return Ok(ApiResponse.Ok("User deleted", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    [HttpPost("users/{id:guid}/reset-password")]
    [HasPermission("users:edit")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetUserPassword(Guid id, CancellationToken ct)
    {
        if (id == GetUserId())
            return BadRequest(ApiResponse.Fail("You cannot reset your own password this way — use Settings."));
        await authManager.AdminResetPasswordAsync(id, ct);
        return Ok(ApiResponse.Ok("A temporary password has been emailed to the user.", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    private Guid GetUserId()
    {
        var raw = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        // A missing or malformed subject claim is a bad token → 401, not a 500.
        return Guid.TryParse(raw, out var id) ? id : throw new UnauthorizedException("Invalid token subject.");
    }
}
