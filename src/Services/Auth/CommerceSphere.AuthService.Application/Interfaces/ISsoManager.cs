using CommerceSphere.AuthService.Application.DTOs.Responses;

namespace CommerceSphere.AuthService.Application.Interfaces;

public interface ISsoManager
{
    // Returns the provider's OAuth authorization URL for the given provider plus the state token.
    // The client should redirect the browser to AuthorizationUrl.
    Task<SsoLoginUrlResponse> GetLoginUrlAsync(string provider, string redirectUri, CancellationToken ct = default);

    // Called by the SSO callback endpoint after the provider redirects back with a code.
    // Exchanges the code, finds or creates the local user, and issues our own JWT + refresh token.
    Task<SsoCallbackResult> HandleCallbackAsync(string code, string state, string ipAddress, string correlationId, CancellationToken ct = default);

    // Lists every supported social provider with whether it is enabled (credentials configured),
    // so the client can always render the buttons and enable only the configured ones.
    IReadOnlyList<SsoProviderInfo> GetAvailableProviders();

    // Resolves the original client redirectUri from Redis for the given state token.
    // Used by the callback endpoint to redirect the user to the right place on error
    // (e.g. user clicked Deny on Google) without exposing internal state details.
    Task<string?> GetRedirectUriForErrorAsync(string state, CancellationToken ct = default);
}
