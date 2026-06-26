using CommerceSphere.AuthService.Application.DTOs.Requests;
using CommerceSphere.AuthService.Application.Interfaces;
using CommerceSphere.AuthService.UnitTests.Common;
using CommerceSphere.Shared.Common.Exceptions;
using FluentAssertions;
using Moq;
using Xunit;

namespace CommerceSphere.AuthService.UnitTests.Managers;

public class TwoFactorManagerTests
{
    private readonly TestFactory _f = new();

    public TwoFactorManagerTests()
    {
        _f.Totp.Setup(t => t.GenerateSecret()).Returns("ABCDEFGHIJKLMNOPQRSTUVWXYZ234567");
        _f.Totp.Setup(t => t.GetQrCodeUri(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("otpauth://totp/CommerceSphere:bob@example.com?secret=ABC");
    }

    [Fact]
    public async Task Setup_StoresSecret_ReturnsQrAndSegments()
    {
        var user = _f.SeedUser("bob@example.com");
        var sut = _f.BuildTwoFactorManager();

        var result = await sut.SetupAsync(user.Id);

        result.SecretKey.Should().Be("ABCDEFGHIJKLMNOPQRSTUVWXYZ234567");
        result.QrCodeUri.Should().StartWith("otpauth://");
        result.ManualEntrySegments.Should().HaveCount(8, "32-char secret splits into 4-char groups");
        user.TwoFactorSecret.Should().Be("ABCDEFGHIJKLMNOPQRSTUVWXYZ234567");
        user.IsActiveTwoFactor.Should().BeFalse("not active until confirmed");
    }

    [Fact]
    public async Task ConfirmSetup_ValidCode_EnablesTwoFactor_ReturnsTokens()
    {
        var user = _f.SeedUser("bob@example.com");
        user.SetTwoFactorSecret("SECRET");
        _f.Totp.Setup(t => t.ValidateCode("SECRET", "123456")).Returns(true);
        var sut = _f.BuildTwoFactorManager();

        var tokens = await sut.ConfirmSetupAsync(user.Id, new ConfirmTwoFactorRequest("123456"), "ip");

        tokens.AccessToken.Should().Be("access-token");
        user.IsActiveTwoFactor.Should().BeTrue();
        user.TwoFactorConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task ConfirmSetup_WithoutSetup_ThrowsBusiness()
    {
        var user = _f.SeedUser("bob@example.com"); // no TwoFactorSecret
        var sut = _f.BuildTwoFactorManager();

        var act = () => sut.ConfirmSetupAsync(user.Id, new ConfirmTwoFactorRequest("123456"), "ip");

        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task ConfirmSetup_InvalidCode_ThrowsBusiness()
    {
        var user = _f.SeedUser("bob@example.com");
        user.SetTwoFactorSecret("SECRET");
        _f.Totp.Setup(t => t.ValidateCode("SECRET", It.IsAny<string>())).Returns(false);
        var sut = _f.BuildTwoFactorManager();

        var act = () => sut.ConfirmSetupAsync(user.Id, new ConfirmTwoFactorRequest("000000"), "ip");

        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task Disable_NotEnabled_ThrowsBusiness()
    {
        var user = _f.SeedUser("bob@example.com");
        var sut = _f.BuildTwoFactorManager();

        var act = () => sut.DisableAsync(user.Id, new DisableTwoFactorRequest("123456"));

        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task Disable_ValidCode_TurnsOffTwoFactor()
    {
        var user = _f.SeedUser("bob@example.com");
        user.SetTwoFactorSecret("SECRET");
        user.ConfirmTwoFactor();
        _f.Totp.Setup(t => t.ValidateCode("SECRET", "123456")).Returns(true);
        var sut = _f.BuildTwoFactorManager();

        await sut.DisableAsync(user.Id, new DisableTwoFactorRequest("123456"));

        user.IsActiveTwoFactor.Should().BeFalse();
        user.TwoFactorSecret.Should().BeNull();
    }

    [Fact]
    public async Task VerifyChallenge_ValidTokenAndCode_ReturnsTokens()
    {
        var user = _f.SeedUser("bob@example.com");
        user.SetTwoFactorSecret("SECRET");
        user.ConfirmTwoFactor();
        (Guid, ChallengeType)? payload = (user.Id, ChallengeType.TwoFactor);
        _f.Challenge.Setup(c => c.ValidateAndConsumeAsync("ch", It.IsAny<CancellationToken>())).ReturnsAsync(payload);
        _f.Totp.Setup(t => t.ValidateCode("SECRET", "123456")).Returns(true);
        var sut = _f.BuildTwoFactorManager();

        var tokens = await sut.VerifyChallengeAsync(new TwoFactorChallengeRequest("ch", "123456"), "ip");

        tokens.AccessToken.Should().Be("access-token");
    }

    [Fact]
    public async Task VerifyChallenge_InvalidChallengeToken_ThrowsUnauthorized()
    {
        (Guid, ChallengeType)? none = null;
        _f.Challenge.Setup(c => c.ValidateAndConsumeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(none);
        var sut = _f.BuildTwoFactorManager();

        var act = () => sut.VerifyChallengeAsync(new TwoFactorChallengeRequest("bad", "123456"), "ip");

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task VerifyChallenge_WrongChallengeType_ThrowsUnauthorized()
    {
        var user = _f.SeedUser("bob@example.com");
        (Guid, ChallengeType)? payload = (user.Id, ChallengeType.Otp); // OTP token used on the 2FA endpoint
        _f.Challenge.Setup(c => c.ValidateAndConsumeAsync("ch", It.IsAny<CancellationToken>())).ReturnsAsync(payload);
        var sut = _f.BuildTwoFactorManager();

        var act = () => sut.VerifyChallengeAsync(new TwoFactorChallengeRequest("ch", "123456"), "ip");

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task VerifyChallenge_InvalidCode_ThrowsUnauthorized()
    {
        var user = _f.SeedUser("bob@example.com");
        user.SetTwoFactorSecret("SECRET");
        user.ConfirmTwoFactor();
        (Guid, ChallengeType)? payload = (user.Id, ChallengeType.TwoFactor);
        _f.Challenge.Setup(c => c.ValidateAndConsumeAsync("ch", It.IsAny<CancellationToken>())).ReturnsAsync(payload);
        _f.Totp.Setup(t => t.ValidateCode("SECRET", It.IsAny<string>())).Returns(false);
        var sut = _f.BuildTwoFactorManager();

        var act = () => sut.VerifyChallengeAsync(new TwoFactorChallengeRequest("ch", "000000"), "ip");

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
