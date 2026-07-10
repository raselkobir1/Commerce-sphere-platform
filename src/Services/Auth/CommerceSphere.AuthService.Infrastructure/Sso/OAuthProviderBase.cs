using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CommerceSphere.AuthService.Application.DTOs.Responses;
using CommerceSphere.Shared.Common.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CommerceSphere.AuthService.Infrastructure.Sso;

// Shared OAuth 2.0 "authorization code" plumbing common to every provider:
//   • building the authorization URL,
//   • exchanging the code for tokens at the provider's token endpoint,
//   • small HTTP helpers for reading a provider's userinfo endpoint.
//
// Each concrete provider only declares its endpoints/scope and maps the token
// response to a SsoUserInfo — so a new provider is a short, linear class.
public abstract class OAuthProviderBase(
    HttpClient http, IOptions<SsoOptions> ssoOptions, ILogger logger) : IOAuthProvider
{
    protected readonly HttpClient Http = http;
    protected readonly ILogger Logger = logger;
    private readonly SsoOptions _sso = ssoOptions.Value;

    // ── Declared by each provider ──────────────────────────────────────────────
    public abstract string Name { get; }
    protected abstract string AuthorizationEndpoint { get; }
    protected abstract string TokenEndpoint { get; }
    protected abstract string Scope { get; }

    // Turn the exchanged tokens into a verified user identity. OIDC providers (Google) read the
    // id_token; plain OAuth2 providers (Facebook) call their userinfo endpoint with the access token.
    protected abstract Task<SsoUserInfo> MapUserAsync(OAuthTokens tokens, CancellationToken ct);

    // ── Shared behaviour ───────────────────────────────────────────────────────
    protected OAuthProviderCredentials Credentials => _sso.GetCredentials(Name);

    public string BuildAuthorizationUrl(string state, string callbackUri)
    {
        var query = BuildQuery(new()
        {
            ["client_id"]     = Credentials.ClientId,
            ["redirect_uri"]  = callbackUri,
            ["response_type"] = "code",
            ["scope"]         = Scope,
            ["state"]         = state
        });
        return $"{AuthorizationEndpoint}?{query}";
    }

    public async Task<SsoUserInfo> GetUserInfoAsync(string code, string callbackUri, CancellationToken ct = default)
    {
        var tokens = await ExchangeCodeAsync(code, callbackUri, ct);
        return await MapUserAsync(tokens, ct);
    }

    // Standard authorization_code → token exchange (server-to-server; the code is never logged).
    private async Task<OAuthTokens> ExchangeCodeAsync(string code, string callbackUri, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"]    = "authorization_code",
                ["client_id"]     = Credentials.ClientId,
                ["client_secret"] = Credentials.ClientSecret,
                ["code"]          = code,
                ["redirect_uri"]  = callbackUri
            })
        };
        // Ask for a JSON token response explicitly; some providers otherwise reply form-encoded.
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await Http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            Logger.LogError("{Provider} token exchange failed. Status: {Status}, Body: {Body}",
                Name, response.StatusCode, body);
            // SsoException → 400; the authorization code may be expired, reused, or the secret is wrong.
            throw new SsoException($"{Name} login failed — the authorization code was rejected. Please try again.");
        }

        var tokens = await response.Content.ReadFromJsonAsync<OAuthTokens>(cancellationToken: ct);
        if (tokens is null || string.IsNullOrWhiteSpace(tokens.AccessToken))
            throw new SsoException($"{Name} returned an empty token response. Please try again.");

        return tokens;
    }

    // GET a provider's userinfo endpoint with the bearer access token and return the parsed JSON.
    // A User-Agent is set because some provider APIs reject requests without one.
    protected async Task<JsonElement> GetJsonAsync(string url, string accessToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.UserAgent.ParseAdd("CommerceSphere-Auth");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await Http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            Logger.LogError("{Provider} userinfo request to {Url} failed. Status: {Status}, Body: {Body}",
                Name, url, response.StatusCode, body);
            throw new SsoException($"{Name} login failed — could not read your profile. Please try again.");
        }

        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
    }

    // Minimal URL-encoded query builder — avoids taking a dependency on WebUtilities.
    private static string BuildQuery(Dictionary<string, string> parameters) =>
        string.Join("&", parameters.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
}

// The subset of an OAuth token response we care about. AccessToken is always present;
// IdToken is only returned by OIDC providers (Google) and is null for plain OAuth2 (Facebook).
public sealed record OAuthTokens
{
    [System.Text.Json.Serialization.JsonPropertyName("access_token")]
    public string AccessToken { get; init; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("id_token")]
    public string? IdToken { get; init; }
}
