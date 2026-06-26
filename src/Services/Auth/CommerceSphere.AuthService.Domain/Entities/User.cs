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

    public ICollection<RefreshToken> RefreshTokens { get; private set; } = [];

    // Collection of linked social identities — populated by EF Core via Include() when needed.
    public ICollection<ExternalLogin> ExternalLogins { get; private set; } = [];

    // Private constructor enforces that users can only be created through the factory method,
    // keeping invariant validation in one place (DDD pattern). EF Core uses this constructor
    // when materializing entities from the database via reflection.
    private User() { }

    public static User Create(string email, string passwordHash, string firstName, string lastName, string role = "Customer")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        return new User
        {
            // Normalise to lowercase so email lookups are case-insensitive without a DB collation change.
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            Role = role
        };
    }

    // Factory for users who register via SSO — they have no local password.
    // Storing an empty PasswordHash is intentional: BCrypt.Verify will always return false,
    // so SSO-only users can never authenticate through the password login endpoint.
    public static User CreateFromSso(string email, string firstName, string lastName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        return new User
        {
            Email = email.ToLowerInvariant(),
            PasswordHash = string.Empty,
            FirstName = string.IsNullOrWhiteSpace(firstName) ? "Unknown" : firstName,
            LastName = string.IsNullOrWhiteSpace(lastName) ? "User" : lastName,
            Role = "Customer"
        };
    }

    public void RecordLogin() => LastLoginAt = DateTime.UtcNow;

    public void Deactivate()
    {
        IsActive = false;
        SetUpdated();
    }

    public void UpdateProfile(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
        SetUpdated();
    }
}
