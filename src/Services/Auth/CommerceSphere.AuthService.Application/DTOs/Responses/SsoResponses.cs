namespace CommerceSphere.AuthService.Application.DTOs.Responses;

// Returned to the client when they request an SSO login URL.
// The client should redirect the user's browser to AuthorizationUrl to begin the social login flow.
public record SsoLoginUrlResponse(
    string Provider,
    string AuthorizationUrl,     // Full provider OAuth authorization URL to redirect the browser to
    string State                 // Opaque value stored server-side; returned by the provider to verify the callback
);

// Carries the parsed user identity returned by a social provider after a successful login.
// Used internally between SsoService and SsoManager — not sent to the client.
public record SsoUserInfo(
    string Sub,         // The provider's stable unique user ID (unchanged even if the user changes email)
    string Email,
    string FirstName,
    string LastName
);

// Returned from SsoManager to the controller after a successful callback.
// The controller uses RedirectUri to send the browser back to the client app with the tokens.
public record SsoCallbackResult(
    AuthTokenResponse Tokens,
    string RedirectUri
);

// One entry in the SSO provider catalog exposed to clients. Every provider the backend supports is
// listed so the UI can always render its button; Enabled is false until that provider's credentials
// are configured, so the frontend can show it as "coming soon" rather than hiding it.
public record SsoProviderInfo(
    string Name,        // lowercase provider key, e.g. "google"
    bool Enabled        // true once ClientId + ClientSecret are configured for this provider
);
