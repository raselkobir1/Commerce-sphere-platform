namespace CommerceSphere.AuthService.Application.DTOs.Responses;

// Returned to the client when they request an SSO login URL.
// The client should redirect the user's browser to AuthorizationUrl to begin the social login flow.
public record SsoLoginUrlResponse(
    string Provider,
    string AuthorizationUrl,     // Full Keycloak OIDC authorization URL to redirect the browser to
    string State                 // Opaque value stored server-side; returned by Keycloak to verify the callback
);

// Carries the parsed user identity from Keycloak's id_token after a successful social login.
// Used internally between KeycloakService and SsoManager — not sent to the client.
public record KeycloakUserInfo(
    string Sub,         // Keycloak's internal user ID (stable, even if user changes email)
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
