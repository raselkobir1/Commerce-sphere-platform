using System.Text.Json;
using CommerceSphere.AuthService.Application.DTOs.Responses;
using CommerceSphere.AuthService.Application.Interfaces;
using CommerceSphere.Shared.Common.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CommerceSphere.AuthService.Infrastructure.Sso;

// Provider-agnostic orchestrator for the social login flow. It owns everything that is the same
// for every provider — CSRF state in Redis, redirect-URI safety checks, provider lookup — and
// delegates the provider-specific OAuth details to the matching IOAuthProvider strategy.
public class SsoService : ISsoService
{
    private readonly IReadOnlyDictionary<string, IOAuthProvider> _providers;
    private readonly SsoOptions _opts;
    private readonly IDatabase _redis;
    private readonly ILogger<SsoService> _logger;

    public SsoService(
        IEnumerable<IOAuthProvider> providers,
        IConnectionMultiplexer redis,
        IOptions<SsoOptions> options,
        ILogger<SsoService> logger)
    {
        _providers = providers.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        _opts = options.Value;
        _redis = redis.GetDatabase();
        _logger = logger;
    }

    private TimeSpan StateTtl => TimeSpan.FromMinutes(_opts.StateTtlMinutes);

    // State is stored in Redis as "sso:state:{state}" → JSON of SsoState.
    private static string StateKey(string state) => $"sso:state:{state}";

    public async Task<SsoLoginUrlResponse> BuildLoginUrlAsync(
        string provider, string redirectUri, CancellationToken ct = default)
    {
        var oauth = ResolveProvider(provider);

        // SECURITY: only allow redirecting tokens back to a configured frontend origin. Otherwise an
        // attacker could send a victim an SSO link with redirectUri=https://evil.com and harvest the
        // access/refresh tokens that the callback appends to that URL (open redirect → token theft).
        if (!_opts.IsRedirectUriAllowed(redirectUri))
            throw new SsoException("The provided redirectUri is not an allowed callback URL.");

        // Random opaque state token (CSRF protection — verified in ProcessCallbackAsync).
        var state = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');

        // Persist state → { provider, redirectUri }; it self-expires so abandoned logins are cleaned up.
        var stateJson = JsonSerializer.Serialize(new SsoState(oauth.Name, redirectUri));
        await _redis.StringSetAsync(StateKey(state), stateJson, StateTtl);

        var authUrl = oauth.BuildAuthorizationUrl(state, _opts.CallbackUri);
        _logger.LogDebug("Built SSO login URL for provider {Provider}", oauth.Name);

        return new SsoLoginUrlResponse(oauth.Name, authUrl, state);
    }

    public async Task<(SsoUserInfo UserInfo, string Provider, string RedirectUri)> ProcessCallbackAsync(
        string code, string state, CancellationToken ct = default)
    {
        // --- Validate and consume state (CSRF check) ---
        var stateJson = await _redis.StringGetAsync(StateKey(state));
        if (stateJson.IsNullOrEmpty)
            throw new SsoException("SSO state token is invalid or expired. Please restart the login flow.");

        // State is single-use: delete it immediately so it cannot be replayed.
        await _redis.KeyDeleteAsync(StateKey(state));

        var ssoState = JsonSerializer.Deserialize<SsoState>(stateJson!)
            ?? throw new SsoException("Failed to read SSO state. Please restart the login flow.");

        var oauth = ResolveProvider(ssoState.Provider);
        var userInfo = await oauth.GetUserInfoAsync(code, _opts.CallbackUri, ct);

        _logger.LogInformation(
            "SSO code exchange successful. Provider: {Provider}, Sub: {Sub}", oauth.Name, userInfo.Sub);

        return (userInfo, oauth.Name, ssoState.RedirectUri);
    }

    // Every supported provider is listed (so the UI can always render its button); Enabled reflects
    // whether that provider's credentials are configured yet.
    public IReadOnlyList<SsoProviderInfo> GetProviders() =>
        _providers.Values
            .Select(p => new SsoProviderInfo(p.Name, _opts.IsProviderConfigured(p.Name)))
            .OrderBy(p => p.Name)
            .ToList();

    // Non-destructive read: returns the original redirectUri without consuming the state token,
    // so the callback can still send the user to the right place when reporting an error.
    public async Task<string?> PeekRedirectUriAsync(string state, CancellationToken ct = default)
    {
        var value = await _redis.StringGetAsync(StateKey(state));
        if (value.IsNullOrEmpty) return null;

        return JsonSerializer.Deserialize<SsoState>(value!)?.RedirectUri;
    }

    // Resolves a configured provider by name or fails with a clear, client-safe message.
    private IOAuthProvider ResolveProvider(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
            throw new BusinessException("Provider name is required.");

        var name = provider.ToLowerInvariant();
        if (_providers.TryGetValue(name, out var oauth) && _opts.IsProviderConfigured(name))
            return oauth;

        var enabled = GetProviders().Where(p => p.Enabled).Select(p => p.Name).ToList();
        throw new BusinessException(enabled.Count == 0
            ? $"SSO provider '{provider}' is not configured. No social login providers are currently enabled."
            : $"SSO provider '{provider}' is not configured. Available: {string.Join(", ", enabled)}.");
    }
}

// The value stored in Redis for each state token.
internal sealed record SsoState(string Provider, string RedirectUri);
