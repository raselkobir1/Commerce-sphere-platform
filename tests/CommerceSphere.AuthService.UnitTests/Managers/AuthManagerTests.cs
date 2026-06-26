using CommerceSphere.AuthService.Application.DTOs.Requests;
using CommerceSphere.AuthService.Application.DTOs.Responses;
using CommerceSphere.AuthService.Application.Interfaces;
using CommerceSphere.AuthService.Domain.Entities;
using CommerceSphere.AuthService.UnitTests.Common;
using CommerceSphere.Shared.Common.Exceptions;
using FluentAssertions;
using Moq;
using Xunit;

namespace CommerceSphere.AuthService.UnitTests.Managers;

public class AuthManagerTests
{
    private readonly TestFactory _f = new();

    // ── Register ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_PersistsUser_IssuesTokens_PublishesEvent_SendsEmail()
    {
        var sut = _f.BuildAuthManager();
        var req = new RegisterRequest("New@Example.com", "Passw0rd!", "New", "User");

        var result = await sut.RegisterAsync(req, "127.0.0.1", "corr-1");

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.User.Email.Should().Be("new@example.com");
        result.User.EmailVerified.Should().BeFalse();

        _f.Uow.UsersStore.Items.Should().ContainSingle();
        _f.Uow.RefreshTokensStore.Items.Should().ContainSingle();
        _f.Events.Verify(e => e.PublishUserCreatedAsync(It.IsAny<Shared.Contracts.Events.Auth.UserCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        _f.Email.Verify(e => e.SendEmailVerificationAsync("new@example.com", "New", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ThrowsConflict()
    {
        _f.SeedUser("dupe@example.com");
        var sut = _f.BuildAuthManager();
        var req = new RegisterRequest("dupe@example.com", "Passw0rd!", "X", "Y");

        var act = () => sut.RegisterAsync(req, "ip", "corr");

        await act.Should().ThrowAsync<ConflictException>();
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_NoSecondFactor_ReturnsSucceeded()
    {
        _f.SeedUser("bob@example.com", "Passw0rd!");
        var sut = _f.BuildAuthManager();

        var result = await sut.LoginAsync(new LoginRequest("bob@example.com", "Passw0rd!"), "ip", "corr");

        result.Should().BeOfType<LoginSucceeded>();
        ((LoginSucceeded)result).Tokens.AccessToken.Should().Be("access-token");
        _f.Uow.RefreshTokensStore.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Login_UnknownEmail_ThrowsUnauthorized()
    {
        var sut = _f.BuildAuthManager();

        var act = () => sut.LoginAsync(new LoginRequest("ghost@example.com", "x"), "ip", "corr");

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Login_WrongPassword_RecordsFailedAttempt_AndThrows()
    {
        var user = _f.SeedUser("bob@example.com", "Passw0rd!");
        var sut = _f.BuildAuthManager();

        var act = () => sut.LoginAsync(new LoginRequest("bob@example.com", "WRONG"), "ip", "corr");

        await act.Should().ThrowAsync<UnauthorizedException>();
        user.FailedLoginAttempts.Should().Be(1);
    }

    [Fact]
    public async Task Login_WhenLockedOut_ThrowsUnauthorized()
    {
        var user = _f.SeedUser("bob@example.com", "Passw0rd!");
        for (var i = 0; i < 5; i++) user.RecordFailedLogin();
        var sut = _f.BuildAuthManager();

        var act = () => sut.LoginAsync(new LoginRequest("bob@example.com", "Passw0rd!"), "ip", "corr");

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*locked*");
    }

    [Fact]
    public async Task Login_InactiveAccount_ThrowsUnauthorized()
    {
        var user = _f.SeedUser("bob@example.com", "Passw0rd!");
        user.Deactivate();
        var sut = _f.BuildAuthManager();

        var act = () => sut.LoginAsync(new LoginRequest("bob@example.com", "Passw0rd!"), "ip", "corr");

        await act.Should().ThrowAsync<UnauthorizedException>().WithMessage("*deactivated*");
    }

    [Fact]
    public async Task Login_With2FAConfirmed_ReturnsTwoFactorChallenge()
    {
        var user = _f.SeedUser("bob@example.com", "Passw0rd!");
        user.SetTwoFactorSecret("SECRET");
        user.ConfirmTwoFactor();
        _f.Challenge.Setup(c => c.CreateAsync(user.Id, ChallengeType.TwoFactor, It.IsAny<CancellationToken>()))
            .ReturnsAsync("challenge-2fa");
        var sut = _f.BuildAuthManager();

        var result = await sut.LoginAsync(new LoginRequest("bob@example.com", "Passw0rd!"), "ip", "corr");

        result.Should().BeOfType<LoginNeedsTwoFactor>();
        ((LoginNeedsTwoFactor)result).ChallengeToken.Should().Be("challenge-2fa");
        _f.Uow.RefreshTokensStore.Items.Should().BeEmpty("no session issued until the challenge is solved");
    }

    [Fact]
    public async Task Login_WithOtpEnabled_GeneratesCode_SendsEmail_ReturnsOtpChallenge()
    {
        var user = _f.SeedUser("bob@example.com", "Passw0rd!");
        user.EnableOtpAuth();
        _f.Otp.Setup(o => o.GenerateAndStoreAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync("123456");
        _f.Challenge.Setup(c => c.CreateAsync(user.Id, ChallengeType.Otp, It.IsAny<CancellationToken>()))
            .ReturnsAsync("challenge-otp");
        var sut = _f.BuildAuthManager();

        var result = await sut.LoginAsync(new LoginRequest("bob@example.com", "Passw0rd!"), "ip", "corr");

        result.Should().BeOfType<LoginNeedsOtp>();
        ((LoginNeedsOtp)result).ChallengeToken.Should().Be("challenge-otp");
        _f.Email.Verify(e => e.SendOtpAsync("bob@example.com", "Test", "123456", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_When2FAAndOtpBothEnabled_2FATakesPriority()
    {
        var user = _f.SeedUser("bob@example.com", "Passw0rd!");
        user.SetTwoFactorSecret("SECRET");
        user.ConfirmTwoFactor();
        user.EnableOtpAuth();
        _f.Challenge.Setup(c => c.CreateAsync(user.Id, ChallengeType.TwoFactor, It.IsAny<CancellationToken>()))
            .ReturnsAsync("challenge-2fa");
        var sut = _f.BuildAuthManager();

        var result = await sut.LoginAsync(new LoginRequest("bob@example.com", "Passw0rd!"), "ip", "corr");

        result.Should().BeOfType<LoginNeedsTwoFactor>();
        _f.Otp.Verify(o => o.GenerateAndStoreAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Refresh / Revoke ────────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshToken_RotatesToken_RevokingTheOldOne()
    {
        var user = _f.SeedUser("bob@example.com", "Passw0rd!");
        var existing = RefreshToken.Create(user.Id, "ip");
        _f.Uow.RefreshTokensStore.Items.Add(existing);
        var sut = _f.BuildAuthManager();

        var result = await sut.RefreshTokenAsync(new RefreshTokenRequest(existing.Token), "ip");

        result.RefreshToken.Should().NotBe(existing.Token);
        existing.IsRevoked.Should().BeTrue();
        existing.ReplacedByToken.Should().Be(result.RefreshToken);
        _f.Uow.RefreshTokensStore.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task RefreshToken_UnknownToken_ThrowsUnauthorized()
    {
        var sut = _f.BuildAuthManager();

        var act = () => sut.RefreshTokenAsync(new RefreshTokenRequest("nope"), "ip");

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task RefreshToken_RevokedToken_ThrowsUnauthorized()
    {
        var user = _f.SeedUser("bob@example.com", "Passw0rd!");
        var revoked = RefreshToken.Create(user.Id, "ip");
        revoked.Revoke();
        _f.Uow.RefreshTokensStore.Items.Add(revoked);
        var sut = _f.BuildAuthManager();

        var act = () => sut.RefreshTokenAsync(new RefreshTokenRequest(revoked.Token), "ip");

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task RevokeToken_ActiveToken_MarksRevoked()
    {
        var user = _f.SeedUser("bob@example.com", "Passw0rd!");
        var token = RefreshToken.Create(user.Id, "ip");
        _f.Uow.RefreshTokensStore.Items.Add(token);
        var sut = _f.BuildAuthManager();

        await sut.RevokeTokenAsync(new RevokeTokenRequest(token.Token));

        token.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeToken_Unknown_ThrowsNotFound()
    {
        var sut = _f.BuildAuthManager();

        var act = () => sut.RevokeTokenAsync(new RevokeTokenRequest("missing"));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task RevokeToken_AlreadyRevoked_ThrowsBusiness()
    {
        var user = _f.SeedUser("bob@example.com", "Passw0rd!");
        var token = RefreshToken.Create(user.Id, "ip");
        token.Revoke();
        _f.Uow.RefreshTokensStore.Items.Add(token);
        var sut = _f.BuildAuthManager();

        var act = () => sut.RevokeTokenAsync(new RevokeTokenRequest(token.Token));

        await act.Should().ThrowAsync<BusinessException>();
    }

    // ── Challenge completion / queries ──────────────────────────────────────────

    [Fact]
    public async Task CompleteLoginForChallenge_IssuesTokens()
    {
        var user = _f.SeedUser("bob@example.com", "Passw0rd!");
        var sut = _f.BuildAuthManager();

        var result = await sut.CompleteLoginForChallengeAsync(user.Id, "ip");

        result.Should().BeOfType<LoginSucceeded>();
        _f.Uow.RefreshTokensStore.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task CompleteLoginForChallenge_InactiveUser_ThrowsUnauthorized()
    {
        var user = _f.SeedUser("bob@example.com", "Passw0rd!");
        user.Deactivate();
        var sut = _f.BuildAuthManager();

        var act = () => sut.CompleteLoginForChallengeAsync(user.Id, "ip");

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task GetUserById_Unknown_ThrowsNotFound()
    {
        var sut = _f.BuildAuthManager();

        var act = () => sut.GetUserByIdAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetUsers_ReturnsPagedResult()
    {
        _f.SeedUser("a@example.com");
        _f.SeedUser("b@example.com");
        var sut = _f.BuildAuthManager();

        var result = await sut.GetUsersAsync(new Shared.Common.Models.PagedRequest { PageNumber = 1, PageSize = 10 });

        result.TotalRecords.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }
}
