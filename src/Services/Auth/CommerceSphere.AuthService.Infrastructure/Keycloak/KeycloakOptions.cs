namespace CommerceSphere.AuthService.Infrastructure.Keycloak;

// Typed configuration bound from the "Keycloak" section in appsettings.json.
// All Keycloak communication settings live here — change them without touching code.
public class KeycloakOptions
{
    // Base URL of the Keycloak realm, e.g. "http://keycloak:8080/realms/commerce-sphere".
    // In Docker: uses internal DNS name "keycloak". For local dev: "http://localhost:8080/realms/...".
    public string Authority { get; set; } = string.Empty;

    // The Keycloak client ID registered under the realm.
    public string ClientId { get; set; } = string.Empty;

    // The Keycloak client secret — treat this like a password; never commit the real value.
    public string ClientSecret { get; set; } = string.Empty;

    // The publicly-reachable base URL for our Auth Service (or API Gateway).
    // Used to build the redirect_uri that Keycloak will call back after social login.
    // In Docker: "http://localhost:5000" (via API Gateway). Local dev: "http://localhost:5211".
    public string CallbackBaseUrl { get; set; } = string.Empty;

    // Path appended to CallbackBaseUrl to form the full redirect_uri registered in Keycloak.
    public string CallbackPath { get; set; } = "/api/auth/sso/callback";

    // Social identity provider aliases that must match aliases configured in the Keycloak admin console.
    // Keycloak 24.0 has built-in support for: google, github, facebook, microsoft, linkedin.
    // Twitter/X is NOT a built-in Keycloak 24 provider — add it manually as a generic OAuth2
    // provider in the Keycloak admin UI before including "twitter" here.
    public List<string> Providers { get; set; } = ["google", "github", "facebook"];

    // How long the SSO state token stays valid in Redis while the user completes the social login.
    // 10 minutes is generous — OAuth flows rarely take longer. Increase if users have slow networks.
    public int StateTtlMinutes { get; set; } = 10;

    // Full redirect URI sent to Keycloak and registered as an allowed redirect in the Keycloak client.
    public string CallbackUri => $"{CallbackBaseUrl.TrimEnd('/')}{CallbackPath}";

    // OIDC standard endpoints derived from the authority.
    public string TokenEndpoint => $"{Authority}/protocol/openid-connect/token";
    public string AuthorizationEndpoint => $"{Authority}/protocol/openid-connect/auth";

    // Fail fast at startup if required values are missing rather than confusing errors at login time.
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Authority))
            throw new InvalidOperationException("Keycloak:Authority is required. Set it to your Keycloak realm URL.");
        if (string.IsNullOrWhiteSpace(ClientId))
            throw new InvalidOperationException("Keycloak:ClientId is required.");
        if (string.IsNullOrWhiteSpace(ClientSecret) || ClientSecret == "CHANGE_ME_IN_KEYCLOAK_ADMIN")
            throw new InvalidOperationException("Keycloak:ClientSecret must be set to the actual client secret from the Keycloak admin console.");
        if (string.IsNullOrWhiteSpace(CallbackBaseUrl))
            throw new InvalidOperationException("Keycloak:CallbackBaseUrl is required (e.g. http://localhost:5000).");
    }
}
