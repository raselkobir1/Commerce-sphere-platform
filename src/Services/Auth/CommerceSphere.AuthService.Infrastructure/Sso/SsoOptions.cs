namespace CommerceSphere.AuthService.Infrastructure.Sso;

// Typed configuration bound from the "Sso" section in appsettings.json / environment variables.
// Holds the settings shared across every social provider plus the per-provider client credentials.
// Example (env vars): Sso__CallbackBaseUrl, Sso__Providers__google__ClientId, ...
public class SsoOptions
{
    // Publicly-reachable base URL of THIS Auth Service (or the API Gateway in front of it).
    // Used to build the redirect_uri that each OAuth provider calls back after login.
    // In Docker: "http://localhost:5000" (via API Gateway). Local dev: "http://localhost:5211".
    public string CallbackBaseUrl { get; set; } = string.Empty;

    // Path appended to CallbackBaseUrl to form the OAuth redirect_uri.
    // This exact URL must be registered as an authorized redirect URI in each provider's console.
    public string CallbackPath { get; set; } = "/api/auth/sso/callback";

    // How long the anti-CSRF state token stays valid in Redis while the user completes the login.
    // 10 minutes is generous — OAuth flows rarely take longer.
    public int StateTtlMinutes { get; set; } = 10;

    // SECURITY: exact frontend origins (scheme://host[:port]) allowed to receive the tokens on the
    // final SSO redirect. Without this, the caller-supplied redirectUri is an OPEN REDIRECT that
    // leaks access/refresh tokens to any attacker-controlled URL. Empty = all redirects refused
    // (fail closed). Configure via Sso:AllowedRedirectUris.
    public List<string> AllowedRedirectUris { get; set; } = [];

    // Per-provider client credentials, keyed by provider name (e.g. "google", "facebook").
    // A provider is only offered for login once both its ClientId and ClientSecret are supplied,
    // so unconfigured providers simply never appear — no code change needed to enable or disable one.
    public Dictionary<string, OAuthProviderCredentials> Providers { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    // The full OAuth redirect_uri sent to providers and registered in their consoles.
    public string CallbackUri => $"{CallbackBaseUrl.TrimEnd('/')}{CallbackPath}";

    // True only when the provider exists in config AND has both ClientId and ClientSecret set.
    public bool IsProviderConfigured(string provider) =>
        Providers.TryGetValue(provider, out var creds) && creds.IsConfigured;

    // Returns the credentials for a configured provider, or throws if it is missing/incomplete.
    public OAuthProviderCredentials GetCredentials(string provider) =>
        Providers.TryGetValue(provider, out var creds) && creds.IsConfigured
            ? creds
            : throw new InvalidOperationException(
                $"SSO provider '{provider}' is not configured. Set Sso:Providers:{provider}:ClientId and :ClientSecret.");

    // True only when redirectUri's origin exactly matches a configured allowed origin. Comparing the
    // parsed origin (not a string prefix) avoids bypasses like "http://localhost:4300.evil.com".
    public bool IsRedirectUriAllowed(string redirectUri)
    {
        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri))
            return false;
        var origin = uri.GetLeftPart(UriPartial.Authority);
        return AllowedRedirectUris.Any(a =>
            string.Equals(a.TrimEnd('/'), origin, StringComparison.OrdinalIgnoreCase));
    }
}

// The client credentials issued by a single OAuth provider's developer console.
// Treat ClientSecret like a password — never commit the real value.
public class OAuthProviderCredentials
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}
