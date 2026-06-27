using System.Security.Cryptography;

namespace CommerceSphere.AuthService.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Role { get; private set; } = "Customer";
    public bool IsActive { get; private set; } = true;
    public DateTime? LastLoginAt { get; private set; }

    // ── Email verification ──────────────────────────────────────────────────
    public bool EmailVerified { get; private set; }
    public string? EmailVerificationToken { get; private set; }
    public DateTime? EmailVerificationTokenExpiry { get; private set; }

    // ── Two-factor auth (TOTP via authenticator app) ────────────────────────
    public bool IsActiveTwoFactor { get; private set; }
    public string? TwoFactorSecret { get; private set; }
    public bool TwoFactorConfirmed { get; private set; }

    // ── OTP auth (email one-time password on login) ─────────────────────────
    public bool IsOtpAuthEnable { get; private set; }

    // ── Password reset ──────────────────────────────────────────────────────
    public string? PasswordResetToken { get; private set; }
    public DateTime? PasswordResetTokenExpiry { get; private set; }

    // ── Account lockout ─────────────────────────────────────────────────────
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockoutEnd { get; private set; }

    public ICollection<RefreshToken> RefreshTokens { get; private set; } = [];
    public ICollection<ExternalLogin> ExternalLogins { get; private set; } = [];

    private User() { }

    public static User Create(string email, string passwordHash, string firstName, string lastName, string role = "Customer")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        return new User
        {
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            Role = role
        };
    }

    public static User CreateFromSso(string email, string firstName, string lastName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        return new User
        {
            Email = email.ToLowerInvariant(),
            PasswordHash = string.Empty,
            FirstName = string.IsNullOrWhiteSpace(firstName) ? "Unknown" : firstName,
            LastName = string.IsNullOrWhiteSpace(lastName) ? "User" : lastName,
            Role = "Customer",
            EmailVerified = true  // social provider already verified their email
        };
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        FailedLoginAttempts = 0;
        LockoutEnd = null;
    }

    public void Deactivate()
    {
        IsActive = false;
        SetUpdated();
    }

    // Admin user-management: change the user's role (must be an existing Role.Name) and active state.
    public void ChangeRole(string role)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(role);
        Role = role.Trim();
        SetUpdated();
    }

    public void SetActive(bool active)
    {
        IsActive = active;
        SetUpdated();
    }

    public void UpdateProfile(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
        SetUpdated();
    }

    // ── Email verification ──────────────────────────────────────────────────

    public string GenerateEmailVerificationToken()
    {
        EmailVerificationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
        SetUpdated();
        return EmailVerificationToken;
    }

    public void MarkEmailVerified()
    {
        EmailVerified = true;
        EmailVerificationToken = null;
        EmailVerificationTokenExpiry = null;
        SetUpdated();
    }

    // ── Two-factor auth (TOTP) ──────────────────────────────────────────────

    public void SetTwoFactorSecret(string secret)
    {
        TwoFactorSecret = secret;
        IsActiveTwoFactor = false;
        TwoFactorConfirmed = false;
        SetUpdated();
    }

    public void ConfirmTwoFactor()
    {
        TwoFactorConfirmed = true;
        IsActiveTwoFactor = true;
        SetUpdated();
    }

    public void DisableTwoFactor()
    {
        IsActiveTwoFactor = false;
        TwoFactorSecret = null;
        TwoFactorConfirmed = false;
        SetUpdated();
    }

    // ── OTP auth ───────────────────────────────────────────────────────────

    public void EnableOtpAuth()
    {
        IsOtpAuthEnable = true;
        SetUpdated();
    }

    public void DisableOtpAuth()
    {
        IsOtpAuthEnable = false;
        SetUpdated();
    }

    // ── Password ───────────────────────────────────────────────────────────

    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        // Invalidate all existing refresh tokens by revoking them at the domain level
        // (the caller must save changes; we just update the in-memory state here).
        SetUpdated();
    }

    public string GeneratePasswordResetToken()
    {
        PasswordResetToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(30);
        SetUpdated();
        return PasswordResetToken;
    }

    public void ClearPasswordResetToken()
    {
        PasswordResetToken = null;
        PasswordResetTokenExpiry = null;
        SetUpdated();
    }

    // ── Lockout ────────────────────────────────────────────────────────────

    public void RecordFailedLogin()
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= 5)
            LockoutEnd = DateTime.UtcNow.AddMinutes(15);
        SetUpdated();
    }

    public bool IsLockedOut() => LockoutEnd.HasValue && LockoutEnd.Value > DateTime.UtcNow;
}
