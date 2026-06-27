using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommerceSphere.AuthService.Application.DTOs.Responses;
using CommerceSphere.AuthService.Application.Interfaces;
using CommerceSphere.Shared.Common.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CommerceSphere.AuthService.Infrastructure.Keycloak;

// Handles all HTTP communication with the Keycloak server and stores/retrieves
// the short-lived SSO state token in Redis to prevent CSRF during the OAuth dance.
public class KeycloakService(
    HttpClient httpClient,
    IConnectionMultiplexer redis,
    IOptions<KeycloakOptions> options,
    ILogger<KeycloakService> logger) : IKeycloakService
{
    private readonly KeycloakOptions _opts = options.Value;
    private readonly IDatabase _redisDb = redis.GetDatabase();
    // TTL comes from config so it can be tuned without redeployment.
    private TimeSpan StateTtl => TimeSpan.FromMinutes(_opts.StateTtlMinutes);

    // Redis key prefix for SSO state tokens.
    // State is stored as "sso:state:{state}" → JSON of SsoState.
    private static string StateKey(string state) => $"sso:state:{state}";

    public async Task<SsoLoginUrlResponse> BuildLoginUrlAsync(
        string provider, string redirectUri, CancellationToken ct = default)
    {
        // SECURITY: only allow redirecting tokens back to a configured frontend origin. Otherwise an
        // attacker could send a victim an SSO link with redirectUri=https://evil.com and harvest the
        // access/refresh tokens that the callback appends to that URL (open redirect → token theft).
        if (!_opts.IsRedirectUriAllowed(redirectUri))
            throw new SsoException("The provided redirectUri is not an allowed callback URL.");

        // Generate a random opaque state token (CSRF protection — verified in ProcessCallbackAsync).
        var state = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');

        // Persist state → { provider, redirectUri } in Redis.
        // After 10 minutes the state expires, so stale or abandoned logins are automatically cleaned up.
        var stateJson = JsonSerializer.Serialize(new SsoState(provider.ToLowerInvariant(), redirectUri));
        await _redisDb.StringSetAsync(StateKey(state), stateJson, StateTtl);

        // Build the Keycloak OIDC authorization URL.
        // kc_idp_hint tells Keycloak to skip its own login page and go directly to the named
        // social provider — better UX since the user already picked "Login with Google".
        var authUrl = $"{_opts.AuthorizationEndpoint}" +
                      $"?client_id={Uri.EscapeDataString(_opts.ClientId)}" +
                      $"&redirect_uri={Uri.EscapeDataString(_opts.CallbackUri)}" +
                      $"&response_type=code" +
                      $"&scope=openid%20email%20profile" +
                      $"&state={Uri.EscapeDataString(state)}" +
                      $"&kc_idp_hint={Uri.EscapeDataString(provider.ToLowerInvariant())}";

        logger.LogDebug("Built SSO login URL for provider {Provider}", provider);

        return new SsoLoginUrlResponse(provider.ToLowerInvariant(), authUrl, state);
    }

    public async Task<(KeycloakUserInfo UserInfo, string Provider, string RedirectUri)> ProcessCallbackAsync(
        string code, string state, CancellationToken ct = default)
    {
        // --- Validate and consume state (CSRF check) ---
        var stateJson = await _redisDb.StringGetAsync(StateKey(state));
        if (stateJson.IsNullOrEmpty)
            // SsoException maps to 400 — tells the client to restart the login flow.
            throw new SsoException("SSO state token is invalid or expired. Please restart the login flow.");

        // State is single-use: delete it immediately so it cannot be replayed.
        await _redisDb.KeyDeleteAsync(StateKey(state));

        var ssoState = JsonSerializer.Deserialize<SsoState>(stateJson!)
            ?? throw new SsoException("Failed to read SSO state. Please restart the login flow.");

        // --- Exchange authorization code for Keycloak tokens ---
        // This is a server-to-server call; the code is never logged.
        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"]    = "authorization_code",
            ["client_id"]     = _opts.ClientId,
            ["client_secret"] = _opts.ClientSecret,
            ["code"]          = code,
            ["redirect_uri"]  = _opts.CallbackUri
        });

        var tokenResponse = await httpClient.PostAsync(_opts.TokenEndpoint, tokenRequest, ct);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            var body = await tokenResponse.Content.ReadAsStringAsync(ct);
            logger.LogError("Keycloak token exchange failed. Status: {Status}, Body: {Body}",
                tokenResponse.StatusCode, body);
            // SsoException → 400 Bad Request; the authorization code may be expired or replayed.
            throw new SsoException("SSO login failed — the authorization code was rejected. Please try again.");
        }

        var keycloakTokens = await tokenResponse.Content.ReadFromJsonAsync<KeycloakTokenResponse>(
            cancellationToken: ct)
            ?? throw new SsoException("Keycloak returned an empty token response. Please try again.");

        // --- Extract user identity from the id_token ---
        // The id_token is a JWT issued by Keycloak. We read claims without re-validating the signature
        // here because we received it directly from Keycloak's token endpoint (server-to-server) —
        // there is no opportunity for tampering in this flow.
        var userInfo = ParseIdToken(keycloakTokens.IdToken);

        logger.LogInformation(
            "Keycloak token exchange successful. Provider: {Provider}, Sub: {Sub}",
            ssoState.Provider, userInfo.Sub);

        return (userInfo, ssoState.Provider, ssoState.RedirectUri);
    }

    public IReadOnlyList<string> GetConfiguredProviders() =>
        _opts.Providers.AsReadOnly();

    // Non-destructive read: returns the original redirectUri without consuming the state token.
    // The state is still valid after this call — it will be consumed by ProcessCallbackAsync.
    public async Task<string?> PeekRedirectUriAsync(string state, CancellationToken ct = default)
    {
        var value = await _redisDb.StringGetAsync(StateKey(state));
        if (value.IsNullOrEmpty) return null;

        var ssoState = JsonSerializer.Deserialize<SsoState>(value!);
        return ssoState?.RedirectUri;
    }

    private static KeycloakUserInfo ParseIdToken(string idToken)
    {
        // JwtSecurityTokenHandler reads the JWT without signature validation.
        // Safe here because we obtained the token directly from Keycloak's endpoint.
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(idToken);

        var sub = jwt.Subject
            ?? throw new SsoException("Identity provider did not return a user ID (missing 'sub' claim).");

        var email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value
            ?? throw new SsoException("Identity provider did not return an email address. Ensure the 'email' scope is granted.");

        var firstName = jwt.Claims.FirstOrDefault(c => c.Type == "given_name")?.Value ?? string.Empty;
        var lastName  = jwt.Claims.FirstOrDefault(c => c.Type == "family_name")?.Value ?? string.Empty;

        return new KeycloakUserInfo(sub, email, firstName, lastName);
    }
}

// Keycloak token endpoint response — maps JSON snake_case fields.
internal sealed class KeycloakTokenResponse
{
    [JsonPropertyName("access_token")]  public string AccessToken  { get; init; } = string.Empty;
    [JsonPropertyName("refresh_token")] public string RefreshToken { get; init; } = string.Empty;
    [JsonPropertyName("id_token")]      public string IdToken      { get; init; } = string.Empty;
    [JsonPropertyName("expires_in")]    public int    ExpiresIn    { get; init; }
    [JsonPropertyName("token_type")]    public string TokenType    { get; init; } = string.Empty;
}

// The value stored in Redis for each state token.
internal sealed record SsoState(string Provider, string RedirectUri);
