using CommerceSphere.AuthService.Application.DTOs.Requests;
using CommerceSphere.AuthService.Application.Interfaces;
using CommerceSphere.AuthService.UnitTests.Common;
using CommerceSphere.Shared.Common.Exceptions;
using FluentAssertions;
using Moq;
using Xunit;

namespace CommerceSphere.AuthService.UnitTests.Managers;

public class OtpManagerTests
{
    private readonly TestFactory _f = new();

    [Fact]
    public async Task ToggleOtpAuth_Enable_SetsFlag()
    {
        var user = _f.SeedUser("bob@example.com");
        var sut = _f.BuildOtpManager();

        await sut.ToggleOtpAuthAsync(user.Id, new ToggleOtpRequest(true));

        user.IsOtpAuthEnable.Should().BeTrue();
    }

    [Fact]
    public async Task ToggleOtpAuth_Disable_ClearsFlag()
    {
        var user = _f.SeedUser("bob@example.com");
        user.EnableOtpAuth();
        var sut = _f.BuildOtpManager();

        await sut.ToggleOtpAuthAsync(user.Id, new ToggleOtpRequest(false));

        user.IsOtpAuthEnable.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleOtpAuth_UnknownUser_ThrowsNotFound()
    {
        var sut = _f.BuildOtpManager();

        var act = () => sut.ToggleOtpAuthAsync(Guid.NewGuid(), new ToggleOtpRequest(true));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task VerifyChallenge_ValidTokenAndCode_ReturnsTokens()
    {
        var user = _f.SeedUser("bob@example.com");
        (Guid, ChallengeType)? payload = (user.Id, ChallengeType.Otp);
        _f.Challenge.Setup(c => c.ValidateAndConsumeAsync("ch", It.IsAny<CancellationToken>())).ReturnsAsync(payload);
        _f.Otp.Setup(o => o.ValidateAndConsumeAsync(user.Id, "123456", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = _f.BuildOtpManager();

        var tokens = await sut.VerifyChallengeAsync(new OtpChallengeRequest("ch", "123456"), "ip");

        tokens.AccessToken.Should().Be("access-token");
    }

    [Fact]
    public async Task VerifyChallenge_InvalidChallengeToken_ThrowsUnauthorized()
    {
        (Guid, ChallengeType)? none = null;
        _f.Challenge.Setup(c => c.ValidateAndConsumeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(none);
        var sut = _f.BuildOtpManager();

        var act = () => sut.VerifyChallengeAsync(new OtpChallengeRequest("bad", "123456"), "ip");

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task VerifyChallenge_WrongChallengeType_ThrowsUnauthorized()
    {
        var user = _f.SeedUser("bob@example.com");
        (Guid, ChallengeType)? payload = (user.Id, ChallengeType.TwoFactor); // 2FA token used on OTP endpoint
        _f.Challenge.Setup(c => c.ValidateAndConsumeAsync("ch", It.IsAny<CancellationToken>())).ReturnsAsync(payload);
        var sut = _f.BuildOtpManager();

        var act = () => sut.VerifyChallengeAsync(new OtpChallengeRequest("ch", "123456"), "ip");

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task VerifyChallenge_InvalidOtpCode_ThrowsUnauthorized()
    {
        var user = _f.SeedUser("bob@example.com");
        (Guid, ChallengeType)? payload = (user.Id, ChallengeType.Otp);
        _f.Challenge.Setup(c => c.ValidateAndConsumeAsync("ch", It.IsAny<CancellationToken>())).ReturnsAsync(payload);
        _f.Otp.Setup(o => o.ValidateAndConsumeAsync(user.Id, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var sut = _f.BuildOtpManager();

        var act = () => sut.VerifyChallengeAsync(new OtpChallengeRequest("ch", "000000"), "ip");

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
