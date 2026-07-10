namespace CommerceSphere.AuthService.Application.DTOs.Requests;

// Request to initiate SSO login — the provider must match a configured provider name
// (e.g. "google", "facebook").
// RedirectUri is the client application URL the Auth Service will ultimately redirect the browser
// to after the social login completes, carrying the issued tokens as query parameters.
public record SsoLoginRequest(
    string Provider,
    string RedirectUri
);
