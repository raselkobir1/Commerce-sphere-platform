namespace CommerceSphere.AuthService.Domain.Entities;

// Represents the link between a local User account and a social identity from an external provider.
// A single user can have multiple external logins (e.g., linked both Google and GitHub).
// When a user signs in via SSO, we look up their external_user_id + provider to find the local account.
public class ExternalLogin
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }

    // Provider alias in lowercase — must match the identity provider alias configured in Keycloak
    // (e.g. "google", "github", "facebook", "twitter").
    public string Provider { get; private set; } = string.Empty;

    // The user's stable unique ID within the provider — we store Keycloak's `sub` (subject) claim
    // here because it is stable and unique per user per realm, regardless of email changes.
    public string ExternalUserId { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public User User { get; private set; } = null!;

    private ExternalLogin() { }

    public static ExternalLogin Create(Guid userId, string provider, string externalUserId) =>
        new()
        {
            UserId = userId,
            Provider = provider.ToLowerInvariant(),
            ExternalUserId = externalUserId
        };
}
