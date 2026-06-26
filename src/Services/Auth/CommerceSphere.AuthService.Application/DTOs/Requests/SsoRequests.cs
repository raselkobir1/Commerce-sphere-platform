namespace CommerceSphere.AuthService.Application.DTOs.Requests;

// Request to initiate SSO login — the provider must match a Keycloak identity provider alias
// (e.g. "google", "github", "facebook", "twitter").
// RedirectUri is the client application URL Keycloak will ultimately redirect the browser to
// after the social login completes, carrying the issued tokens as query parameters.
public record SsoLoginRequest(
    string Provider,
    string RedirectUri
);
