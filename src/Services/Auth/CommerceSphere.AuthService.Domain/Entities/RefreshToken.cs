using System.Security.Cryptography;

namespace CommerceSphere.AuthService.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public string Token { get; private set; } = string.Empty;
    public Guid UserId { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }

    // When a token is rotated, we store which token replaced it so we can detect
    // reuse of an old token and trigger a full revocation (refresh token rotation security).
    public string? ReplacedByToken { get; private set; }
    public string CreatedByIp { get; private set; } = string.Empty;

    public User User { get; private set; } = null!;

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    // A token is only usable if it hasn't been explicitly revoked AND hasn't expired.
    public bool IsActive => !IsRevoked && !IsExpired;

    private RefreshToken() { }

    public static RefreshToken Create(Guid userId, string createdByIp, int expiryDays = 7) =>
        new()
        {
            // SECURITY: use a CSPRNG (not Guid.NewGuid, which is not guaranteed unpredictable) for
            // this bearer credential. 32 random bytes → URL-safe Base64.
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
            CreatedByIp = createdByIp
        };

    // Called with replacedByToken when rotating (old token revoked, new one issued).
    // Called without argument when explicitly revoking (logout / security event).
    public void Revoke(string? replacedByToken = null)
    {
        IsRevoked = true;
        ReplacedByToken = replacedByToken;
        SetUpdated();
    }
}
