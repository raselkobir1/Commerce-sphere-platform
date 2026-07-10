using CommerceSphere.AuthService.Application.Interfaces;
using CommerceSphere.Shared.Common.Correlation;
using CommerceSphere.Shared.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace CommerceSphere.AuthService.API.Controllers;

// SSO endpoints handle the two-step OAuth social login flow (direct to each provider):
//
//   Step 1 — /sso/login/{provider}
//     Client calls this to get the provider's authorization URL, then redirects the browser there.
//     The provider shows its own login/consent page (Google, Facebook).
//
//   Step 2 — /sso/callback (called by the browser after the provider redirects back)
//     We exchange the authorization code for user info, create/link a local account,
//     issue our JWT, and redirect the browser to the client's redirectUri with the tokens.
[ApiController]
[Route("api/auth/sso")]
[Produces("application/json")]
public class SsoController(ISsoManager ssoManager) : ControllerBase
{
    // Returns the list of social providers available for SSO login.
    // Clients use this to render the "Login with Google / Facebook / ..." buttons.
    [HttpGet("providers")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public IActionResult GetProviders()
    {
        var providers = ssoManager.GetAvailableProviders();
        return Ok(ApiResponse<object>.Ok(providers, "Available SSO providers"));
    }

    // Step 1: Generate the provider's OAuth authorization URL for the requested provider.
    //
    // The client should redirect the user's browser to AuthorizationUrl.
    // After the social login, the browser will land on /sso/callback.
    //
    // redirectUri — the URL of YOUR frontend application that should receive the
    //   tokens after login (e.g. "http://myapp.com/auth/callback").
    //   This origin must be listed in Sso:AllowedRedirectUris (open-redirect guard).
    [HttpGet("login/{provider}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(
        string provider,
        [FromQuery] string redirectUri,
        CancellationToken ct)
    {
        var result = await ssoManager.GetLoginUrlAsync(provider, redirectUri, ct);
        return Ok(ApiResponse<object>.Ok(result,
            $"Redirect the browser to AuthorizationUrl to begin {provider} login.",
            HttpContext.TraceIdentifier,
            HttpContext.GetCorrelationId()));
    }

    // Step 2: The provider redirects the browser here after the social login completes.
    //
    // This endpoint is called by the USER'S BROWSER (not by the provider server-to-server).
    // The provider appends either:
    //   Success: ?code=...&state=...
    //   Failure: ?error=access_denied&error_description=...&state=...
    //
    // On success: redirects the browser to the client's redirectUri with tokens in query params.
    // On user-deny / provider error: redirects to redirectUri with ?sso_error=... so the
    //   frontend can show a helpful message instead of a blank page.
    //
    // SECURITY NOTE: tokens in query parameters are visible in browser history and server logs.
    // For production consider a short-lived server-side session code pattern instead.
    [HttpGet("callback")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromQuery] string? error_description,
        CancellationToken ct)
    {
        // Handle the case where the user clicked "Deny" on the social provider's consent screen,
        // or the provider itself encountered an error during the OAuth flow.
        if (!string.IsNullOrWhiteSpace(error))
        {
            var description = error_description ?? error;

            // Try to resolve the original redirectUri from Redis so we can send the user
            // back to the right place with an error message. If state is gone (expired / missing),
            // fall back to a minimal JSON error response.
            if (string.IsNullOrWhiteSpace(state))
            {
                return BadRequest(ApiResponse.Fail($"SSO failed: {description}"));
            }

            var errorRedirect = await ssoManager.GetRedirectUriForErrorAsync(state, ct);
            if (string.IsNullOrWhiteSpace(errorRedirect))
                return BadRequest(ApiResponse.Fail($"SSO failed: {description}"));

            return Redirect($"{errorRedirect}?sso_error={Uri.EscapeDataString(description)}");
        }

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
            return BadRequest(ApiResponse.Fail("Missing code or state parameter in SSO callback."));

        var correlationId = HttpContext.GetCorrelationId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var result = await ssoManager.HandleCallbackAsync(code, state, ipAddress, correlationId, ct);

        // Redirect the browser back to the client frontend with the issued tokens.
        // The client reads them from the URL and stores them (memory/sessionStorage — avoid localStorage).
        var tokens = result.Tokens;
        var redirect = $"{result.RedirectUri}" +
                       $"?access_token={Uri.EscapeDataString(tokens.AccessToken)}" +
                       $"&refresh_token={Uri.EscapeDataString(tokens.RefreshToken)}" +
                       $"&expires_at={Uri.EscapeDataString(tokens.ExpiresAt.ToString("O"))}";

        return Redirect(redirect);
    }
}
