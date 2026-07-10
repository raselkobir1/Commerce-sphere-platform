using System.IdentityModel.Tokens.Jwt;
using CommerceSphere.AuthService.Application.DTOs.Responses;
using CommerceSphere.Shared.Common.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CommerceSphere.AuthService.Infrastructure.Sso.Providers;

// Google Sign-In (OpenID Connect).
// Google returns an id_token (a signed JWT) alongside the access token, so the user's
// identity is read straight from that token — no extra userinfo call is needed.
// Console: https://console.cloud.google.com → APIs & Services → Credentials → OAuth client ID.
public sealed class GoogleOAuthProvider(
    HttpClient http, IOptions<SsoOptions> sso, ILogger<GoogleOAuthProvider> logger)
    : OAuthProviderBase(http, sso, logger)
{
    public override string Name => "google";

    protected override string AuthorizationEndpoint => "https://accounts.google.com/o/oauth2/v2/auth";
    protected override string TokenEndpoint         => "https://oauth2.googleapis.com/token";
    protected override string Scope                 => "openid email profile";

    protected override Task<SsoUserInfo> MapUserAsync(OAuthTokens tokens, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tokens.IdToken))
            throw new SsoException("Google did not return an id_token. Ensure the 'openid' scope is granted.");

        // We received the id_token directly from Google's token endpoint over TLS (server-to-server),
        // so reading its claims without re-validating the signature is safe here.
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokens.IdToken);

        var sub = jwt.Subject
            ?? throw new SsoException("Google did not return a user ID (missing 'sub' claim).");
        var email = Claim(jwt, "email")
            ?? throw new SsoException("Google did not return an email address. Ensure the 'email' scope is granted.");

        var firstName = Claim(jwt, "given_name") ?? string.Empty;
        var lastName  = Claim(jwt, "family_name") ?? string.Empty;

        return Task.FromResult(new SsoUserInfo(sub, email, firstName, lastName));
    }

    private static string? Claim(JwtSecurityToken jwt, string type) =>
        jwt.Claims.FirstOrDefault(c => c.Type == type)?.Value;
}
