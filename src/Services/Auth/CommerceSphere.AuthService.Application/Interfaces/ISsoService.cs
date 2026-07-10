using CommerceSphere.AuthService.Application.DTOs.Responses;

namespace CommerceSphere.AuthService.Application.Interfaces;

// Abstracts the OAuth handshake with the social providers so SsoManager stays free of HTTP
// and Redis concerns. Implemented in the Infrastructure layer (SsoService), which delegates
// the provider-specific details to per-provider IOAuthProvider strategies.
public interface ISsoService
{
    // Builds the provider's authorization URL, generates and stores a random state token in Redis
    // (CSRF protection), and returns both the URL and the state.
    Task<SsoLoginUrlResponse> BuildLoginUrlAsync(string provider, string redirectUri, CancellationToken ct = default);

    // Validates the state, exchanges the authorization code for tokens, and returns the parsed
    // user identity plus the original provider and client redirectUri.
    Task<(SsoUserInfo UserInfo, string Provider, string RedirectUri)> ProcessCallbackAsync(
        string code, string state, CancellationToken ct = default);

    // Returns the full catalog of supported providers, each flagged with whether it is Enabled
    // (credentials configured). Clients list them all and enable only the configured ones.
    IReadOnlyList<SsoProviderInfo> GetProviders();

    // Reads the state payload from Redis without deleting it (non-destructive read).
    // Used to recover the redirectUri when an error must be reported back to the client.
    Task<string?> PeekRedirectUriAsync(string state, CancellationToken ct = default);
}
