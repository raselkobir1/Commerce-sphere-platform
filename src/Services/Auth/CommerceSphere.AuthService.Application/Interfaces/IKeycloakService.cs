using CommerceSphere.AuthService.Application.DTOs.Responses;

namespace CommerceSphere.AuthService.Application.Interfaces;

// Abstracts all HTTP communication with the Keycloak server so SsoManager stays
// free of HTTP and Redis concerns. Implemented in the Infrastructure layer.
public interface IKeycloakService
{
    // Builds the Keycloak authorization URL for the given provider, generates and stores
    // a random state token in Redis (10-min TTL), and returns both the URL and the state.
    Task<SsoLoginUrlResponse> BuildLoginUrlAsync(string provider, string redirectUri, CancellationToken ct = default);

    // Exchanges the authorization code for Keycloak tokens, validates the state, and
    // returns the parsed user identity plus the original provider and client redirectUri.
    Task<(KeycloakUserInfo UserInfo, string Provider, string RedirectUri)> ProcessCallbackAsync(
        string code, string state, CancellationToken ct = default);

    // Returns the list of providers configured in KeycloakOptions (e.g. ["google","github"]).
    IReadOnlyList<string> GetConfiguredProviders();

    // Reads the state payload from Redis without deleting it (non-destructive read).
    // Used to recover the redirectUri when an error must be reported back to the client.
    Task<string?> PeekRedirectUriAsync(string state, CancellationToken ct = default);
}
