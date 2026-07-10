using System.Text.Json;
using CommerceSphere.AuthService.Application.DTOs.Responses;
using CommerceSphere.Shared.Common.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CommerceSphere.AuthService.Infrastructure.Sso.Providers;

// Facebook Login. Facebook is OAuth2 — after the token exchange we read the profile from the
// Graph API, explicitly requesting the fields we need. Console: https://developers.facebook.com
// → your App → Facebook Login → Settings (add the callback to Valid OAuth Redirect URIs).
public sealed class FacebookOAuthProvider(
    HttpClient http, IOptions<SsoOptions> sso, ILogger<FacebookOAuthProvider> logger)
    : OAuthProviderBase(http, sso, logger)
{
    public override string Name => "facebook";

    protected override string AuthorizationEndpoint => "https://www.facebook.com/v19.0/dialog/oauth";
    protected override string TokenEndpoint         => "https://graph.facebook.com/v19.0/oauth/access_token";
    protected override string Scope                 => "email public_profile";

    protected override async Task<SsoUserInfo> MapUserAsync(OAuthTokens tokens, CancellationToken ct)
    {
        // Facebook returns only the fields explicitly requested.
        var profile = await GetJsonAsync(
            "https://graph.facebook.com/v19.0/me?fields=id,email,first_name,last_name",
            tokens.AccessToken, ct);

        var sub = GetString(profile, "id")
            ?? throw new SsoException("Facebook did not return a user ID.");

        // Email can be absent if the user registered by phone or denied the email permission.
        var email = GetString(profile, "email")
            ?? throw new SsoException("Facebook did not share an email address. Grant email access and try again.");

        var firstName = GetString(profile, "first_name") ?? string.Empty;
        var lastName  = GetString(profile, "last_name") ?? string.Empty;

        return new SsoUserInfo(sub, email, firstName, lastName);
    }

    private static string? GetString(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}
