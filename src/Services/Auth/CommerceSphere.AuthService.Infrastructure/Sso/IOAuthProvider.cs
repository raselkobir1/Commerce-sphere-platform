using CommerceSphere.AuthService.Application.DTOs.Responses;

namespace CommerceSphere.AuthService.Infrastructure.Sso;

// Strategy for one social login provider (Google, Facebook, ...).
// Each implementation knows that provider's OAuth endpoints and how to turn an
// authorization code into a verified user identity. SsoService picks the right
// one by Name and handles everything provider-agnostic (state, Redis, DB).
//
// To add a new provider: implement this interface, register it in
// InfrastructureExtensions, and add its credentials under Sso:Providers:<name>.
public interface IOAuthProvider
{
    // Lowercase provider key, e.g. "google". Must match the Sso:Providers config key.
    string Name { get; }

    // Builds the provider's authorization URL to which the user's browser is redirected.
    //   state       — opaque anti-CSRF token echoed back on the callback.
    //   callbackUri — our server's redirect_uri (SsoOptions.CallbackUri); must match the token exchange.
    string BuildAuthorizationUrl(string state, string callbackUri);

    // Exchanges the authorization code for tokens and returns the verified user identity.
    //   callbackUri — must be byte-for-byte identical to the one used in BuildAuthorizationUrl.
    Task<SsoUserInfo> GetUserInfoAsync(string code, string callbackUri, CancellationToken ct = default);
}
