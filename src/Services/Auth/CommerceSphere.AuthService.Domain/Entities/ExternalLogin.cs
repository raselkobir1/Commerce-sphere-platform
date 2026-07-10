namespace CommerceSphere.AuthService.Domain.Entities;

// Represents the link between a local User account and a social identity from an external provider.
// A single user can have multiple external logins (e.g., linked both Google and Facebook).
// When a user signs in via SSO, we look up their external_user_id + provider to find the local account.
public class ExternalLogin
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }

    // Provider name in lowercase — the social provider the user signed in with
    // (e.g. "google", "facebook").
    public string Provider { get; private set; } = string.Empty;

    // The user's stable unique ID at the provider (e.g. the OIDC/Graph `sub`/`id`).
    // We key on this rather than email because it never changes even if the user changes email.
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
