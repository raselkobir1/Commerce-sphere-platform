using CommerceSphere.AuthService.Application.Interfaces;
using CommerceSphere.AuthService.Application.Managers;
using CommerceSphere.AuthService.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using BC = BCrypt.Net.BCrypt;

namespace CommerceSphere.AuthService.UnitTests.Common;

// Shared builders + mocks so each manager test reads as arrange/act/assert without ceremony.
public sealed class TestFactory
{
    public FakeUnitOfWork Uow { get; } = new();
    public Mock<IJwtService> Jwt { get; } = new();
    public Mock<IUserEventProducer> Events { get; } = new();
    public Mock<IEmailService> Email { get; } = new();
    public Mock<IChallengeTokenService> Challenge { get; } = new();
    public Mock<IOtpCodeService> Otp { get; } = new();
    public Mock<ITotpService> Totp { get; } = new();

    public TestFactory()
    {
        Jwt.Setup(j => j.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IEnumerable<string>>())).Returns("access-token");
        Jwt.Setup(j => j.GetAccessTokenExpiry()).Returns(DateTime.UtcNow.AddMinutes(60));
    }

    public AuthManager BuildAuthManager() =>
        new(Uow, Jwt.Object, Events.Object, Email.Object, Challenge.Object, Otp.Object,
            NullLogger<AuthManager>.Instance);

    public AccountManager BuildAccountManager() =>
        new(Uow, Email.Object, Challenge.Object, BuildAuthManager(), NullLogger<AccountManager>.Instance);

    public TwoFactorManager BuildTwoFactorManager() =>
        new(Uow, Totp.Object, Challenge.Object, BuildAuthManager(), NullLogger<TwoFactorManager>.Instance);

    public OtpManager BuildOtpManager() =>
        new(Uow, Otp.Object, Challenge.Object, BuildAuthManager(), NullLogger<OtpManager>.Instance);

    // Seeds a persisted, verified user with a known password. Returns the entity for further tweaking.
    public User SeedUser(string email = "user@example.com", string password = "Passw0rd!", string role = "Customer")
    {
        var user = User.Create(email, BC.HashPassword(password), "Test", "User", role);
        user.MarkEmailVerified();
        Uow.UsersStore.Items.Add(user);
        return user;
    }
}
