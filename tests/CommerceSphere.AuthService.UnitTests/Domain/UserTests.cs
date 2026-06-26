using CommerceSphere.AuthService.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace CommerceSphere.AuthService.UnitTests.Domain;

public class UserTests
{
    [Fact]
    public void Create_NormalizesEmailToLowercase_AndDefaultsToCustomer()
    {
        var user = User.Create("Bob@Example.COM", "hash", "Bob", "Smith");

        user.Email.Should().Be("bob@example.com");
        user.Role.Should().Be("Customer");
        user.IsActive.Should().BeTrue();
        user.EmailVerified.Should().BeFalse();
        user.IsActiveTwoFactor.Should().BeFalse();
        user.IsOtpAuthEnable.Should().BeFalse();
        user.Id.Should().NotBe(Guid.Empty);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_ThrowsWhenEmailMissing(string? email)
    {
        var act = () => User.Create(email!, "hash", "Bob", "Smith");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ThrowsWhenPasswordHashMissing()
    {
        var act = () => User.Create("bob@example.com", "", "Bob", "Smith");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateFromSso_MarksEmailVerified_AndFillsBlankNames()
    {
        var user = User.CreateFromSso("alice@example.com", "", "");

        user.EmailVerified.Should().BeTrue();
        user.FirstName.Should().Be("Unknown");
        user.LastName.Should().Be("User");
        user.PasswordHash.Should().BeEmpty();
    }

    [Fact]
    public void RecordFailedLogin_LocksOutAfterFiveAttempts()
    {
        var user = User.Create("bob@example.com", "hash", "Bob", "Smith");

        for (var i = 0; i < 4; i++)
            user.RecordFailedLogin();

        user.IsLockedOut().Should().BeFalse("four failures is below the threshold");
        user.FailedLoginAttempts.Should().Be(4);

        user.RecordFailedLogin(); // 5th

        user.FailedLoginAttempts.Should().Be(5);
        user.IsLockedOut().Should().BeTrue();
        user.LockoutEnd.Should().NotBeNull();
        user.LockoutEnd!.Value.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void RecordLogin_ClearsFailedAttemptsAndLockout()
    {
        var user = User.Create("bob@example.com", "hash", "Bob", "Smith");
        for (var i = 0; i < 5; i++) user.RecordFailedLogin();
        user.IsLockedOut().Should().BeTrue();

        user.RecordLogin();

        user.FailedLoginAttempts.Should().Be(0);
        user.LockoutEnd.Should().BeNull();
        user.IsLockedOut().Should().BeFalse();
        user.LastLoginAt.Should().NotBeNull();
    }

    [Fact]
    public void GenerateEmailVerificationToken_ProducesTokenWith24hExpiry()
    {
        var user = User.Create("bob@example.com", "hash", "Bob", "Smith");

        var token = user.GenerateEmailVerificationToken();

        token.Should().NotBeNullOrWhiteSpace();
        token.Should().MatchRegex("^[0-9a-f]+$", "token is lowercase hex");
        user.EmailVerificationToken.Should().Be(token);
        user.EmailVerificationTokenExpiry.Should().BeCloseTo(DateTime.UtcNow.AddHours(24), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void MarkEmailVerified_SetsFlagAndClearsToken()
    {
        var user = User.Create("bob@example.com", "hash", "Bob", "Smith");
        user.GenerateEmailVerificationToken();

        user.MarkEmailVerified();

        user.EmailVerified.Should().BeTrue();
        user.EmailVerificationToken.Should().BeNull();
        user.EmailVerificationTokenExpiry.Should().BeNull();
    }

    [Fact]
    public void TwoFactorLifecycle_SetConfirmDisable()
    {
        var user = User.Create("bob@example.com", "hash", "Bob", "Smith");

        user.SetTwoFactorSecret("SECRET");
        user.TwoFactorSecret.Should().Be("SECRET");
        user.IsActiveTwoFactor.Should().BeFalse("not active until confirmed");
        user.TwoFactorConfirmed.Should().BeFalse();

        user.ConfirmTwoFactor();
        user.IsActiveTwoFactor.Should().BeTrue();
        user.TwoFactorConfirmed.Should().BeTrue();

        user.DisableTwoFactor();
        user.IsActiveTwoFactor.Should().BeFalse();
        user.TwoFactorConfirmed.Should().BeFalse();
        user.TwoFactorSecret.Should().BeNull();
    }

    [Fact]
    public void OtpAuth_EnableThenDisable()
    {
        var user = User.Create("bob@example.com", "hash", "Bob", "Smith");

        user.EnableOtpAuth();
        user.IsOtpAuthEnable.Should().BeTrue();

        user.DisableOtpAuth();
        user.IsOtpAuthEnable.Should().BeFalse();
    }

    [Fact]
    public void GeneratePasswordResetToken_HasThirtyMinuteExpiry()
    {
        var user = User.Create("bob@example.com", "hash", "Bob", "Smith");

        var token = user.GeneratePasswordResetToken();

        token.Should().NotBeNullOrWhiteSpace();
        user.PasswordResetToken.Should().Be(token);
        user.PasswordResetTokenExpiry.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(30), TimeSpan.FromMinutes(1));

        user.ClearPasswordResetToken();
        user.PasswordResetToken.Should().BeNull();
        user.PasswordResetTokenExpiry.Should().BeNull();
    }

    [Fact]
    public void UpdateProfile_ChangesNamesAndStampsUpdatedAt()
    {
        var user = User.Create("bob@example.com", "hash", "Bob", "Smith");

        user.UpdateProfile("Robert", "Jones");

        user.FirstName.Should().Be("Robert");
        user.LastName.Should().Be("Jones");
        user.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var user = User.Create("bob@example.com", "hash", "Bob", "Smith");

        user.Deactivate();

        user.IsActive.Should().BeFalse();
    }
}
