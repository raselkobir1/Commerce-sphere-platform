using CommerceSphere.AuthService.Application.DTOs.Requests;
using CommerceSphere.AuthService.Application.Interfaces;
using CommerceSphere.Shared.Common.Correlation;
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
            ApiResponse<object>.Ok(result, "Registration successful", HttpContext.TraceIdentifier, correlationId));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.GetCorrelationId();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await authManager.LoginAsync(request, ip, correlationId, ct);
        return Ok(ApiResponse<object>.Ok(result, "Login successful", HttpContext.TraceIdentifier, correlationId));
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
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value ?? throw new UnauthorizedAccessException());
        var user = await authManager.GetUserByIdAsync(userId, ct);
        return Ok(ApiResponse<object>.Ok(user, "User retrieved", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers([FromQuery] PagedRequest paged, CancellationToken ct)
    {
        var result = await authManager.GetUsersAsync(paged, ct);
        return Ok(ApiResponse<object>.Ok(result, "Users retrieved", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }
}
