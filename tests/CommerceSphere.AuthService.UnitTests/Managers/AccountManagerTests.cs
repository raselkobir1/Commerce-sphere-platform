using CommerceSphere.AuthService.Application.DTOs.Requests;
using CommerceSphere.AuthService.Domain.Entities;
using CommerceSphere.AuthService.UnitTests.Common;
using CommerceSphere.Shared.Common.Exceptions;
using FluentAssertions;
using Moq;
using Xunit;
using BC = BCrypt.Net.BCrypt;

namespace CommerceSphere.AuthService.UnitTests.Managers;

public class AccountManagerTests
{
    private readonly TestFactory _f = new();

    // ── Profile ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateProfile_ChangesNames()
    {
        var user = _f.SeedUser();
        var sut = _f.BuildAccountManager();

        var result = await sut.UpdateProfileAsync(user.Id, new UpdateProfileRequest("Robert", "Jones"));

        result.FirstName.Should().Be("Robert");
        result.LastName.Should().Be("Jones");
    }

    [Fact]
    public async Task UpdateProfile_UnknownUser_ThrowsNotFound()
    {
        var sut = _f.BuildAccountManager();

        var act = () => sut.UpdateProfileAsync(Guid.NewGuid(), new UpdateProfileRequest("A", "B"));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── Change password ───────────────────────────────────────────────────────

    [Fact]
    public async Task ChangePassword_CorrectCurrent_UpdatesHash_AndRevokesSessions()
    {
        var user = _f.SeedUser("bob@example.com", "OldPass1!");
        var session = RefreshToken.Create(user.Id, "ip");
        user.RefreshTokens.Add(session);
        var sut = _f.BuildAccountManager();

        await sut.ChangePasswordAsync(user.Id, new ChangePasswordRequest("OldPass1!", "NewPass1!"));

        BC.Verify("NewPass1!", user.PasswordHash).Should().BeTrue();
        session.IsRevoked.Should().BeTrue("other sessions are invalidated on password change");
    }

    [Fact]
    public async Task ChangePassword_WrongCurrent_ThrowsUnauthorized()
    {
        var user = _f.SeedUser("bob@example.com", "OldPass1!");
        var sut = _f.BuildAccountManager();

        var act = () => sut.ChangePasswordAsync(user.Id, new ChangePasswordRequest("WRONG", "NewPass1!"));

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    // ── Forgot / reset password ─────────────────────────────────────────────────

    [Fact]
    public async Task ForgotPassword_UnknownEmail_ReturnsSilently_NoEmailSent()
    {
        var sut = _f.BuildAccountManager();

        await sut.ForgotPasswordAsync(new ForgotPasswordRequest("ghost@example.com"));

        _f.Email.Verify(e => e.SendPasswordResetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ForgotPassword_KnownEmail_GeneratesToken_SendsEmail()
    {
        var user = _f.SeedUser("bob@example.com");
        var sut = _f.BuildAccountManager();

        await sut.ForgotPasswordAsync(new ForgotPasswordRequest("bob@example.com"));

        user.PasswordResetToken.Should().NotBeNullOrWhiteSpace();
        _f.Email.Verify(e => e.SendPasswordResetAsync("bob@example.com", "Test", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetPassword_ValidToken_ChangesPassword_AndClearsToken()
    {
        var user = _f.SeedUser("bob@example.com", "OldPass1!");
        var token = user.GeneratePasswordResetToken();
        var sut = _f.BuildAccountManager();

        await sut.ResetPasswordAsync(new ResetPasswordRequest(token, "BrandNew1!"));

        BC.Verify("BrandNew1!", user.PasswordHash).Should().BeTrue();
        user.PasswordResetToken.Should().BeNull();
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_ThrowsBusiness()
    {
        _f.SeedUser("bob@example.com");
        var sut = _f.BuildAccountManager();

        var act = () => sut.ResetPasswordAsync(new ResetPasswordRequest("not-a-token", "BrandNew1!"));

        await act.Should().ThrowAsync<BusinessException>();
    }

    // ── Email verification ──────────────────────────────────────────────────────

    [Fact]
    public async Task SendVerificationEmail_AlreadyVerified_ThrowsBusiness()
    {
        var user = _f.SeedUser("bob@example.com"); // SeedUser marks verified
        var sut = _f.BuildAccountManager();

        var act = () => sut.SendVerificationEmailAsync(user.Id);

        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task VerifyEmail_ValidToken_MarksVerified()
    {
        var user = User.Create("fresh@example.com", BC.HashPassword("Passw0rd!"), "Fresh", "User");
        var token = user.GenerateEmailVerificationToken();
        _f.Uow.UsersStore.Items.Add(user);
        var sut = _f.BuildAccountManager();

        await sut.VerifyEmailAsync(new VerifyEmailRequest(token));

        user.EmailVerified.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyEmail_InvalidToken_ThrowsBusiness()
    {
        var sut = _f.BuildAccountManager();

        var act = () => sut.VerifyEmailAsync(new VerifyEmailRequest("bad-token"));

        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task ResendVerification_UnknownEmail_ReturnsSilently()
    {
        var sut = _f.BuildAccountManager();

        await sut.ResendVerificationEmailAsync(new ResendVerificationEmailRequest("ghost@example.com"));

        _f.Email.Verify(e => e.SendEmailVerificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Sessions ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetActiveSessions_ReturnsSessionsNewestFirst()
    {
        var user = _f.SeedUser();
        user.RefreshTokens.Add(RefreshToken.Create(user.Id, "1.1.1.1"));
        user.RefreshTokens.Add(RefreshToken.Create(user.Id, "2.2.2.2"));
        var sut = _f.BuildAccountManager();

        var sessions = await sut.GetActiveSessionsAsync(user.Id);

        sessions.Should().HaveCount(2);
        sessions.Should().BeInDescendingOrder(s => s.CreatedAt);
    }

    [Fact]
    public async Task RevokeAllSessions_RevokesEveryActiveToken()
    {
        var user = _f.SeedUser();
        var t1 = RefreshToken.Create(user.Id, "1.1.1.1");
        var t2 = RefreshToken.Create(user.Id, "2.2.2.2");
        user.RefreshTokens.Add(t1);
        user.RefreshTokens.Add(t2);
        var sut = _f.BuildAccountManager();

        await sut.RevokeAllSessionsAsync(user.Id);

        t1.IsRevoked.Should().BeTrue();
        t2.IsRevoked.Should().BeTrue();
    }
}
