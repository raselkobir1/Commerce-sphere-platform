using CommerceSphere.AuthService.Infrastructure.Services;
using FluentAssertions;
using OtpNet;
using Xunit;

namespace CommerceSphere.AuthService.UnitTests.Services;

// Exercises the real TotpService against the real Otp.NET library — no mocking — to prove the
// secret it generates is compatible with a standard authenticator code computed independently.
public class TotpServiceTests
{
    private readonly TotpService _sut = new();

    [Fact]
    public void GenerateSecret_ProducesValidBase32()
    {
        var secret = _sut.GenerateSecret();

        secret.Should().NotBeNullOrWhiteSpace();
        var act = () => Base32Encoding.ToBytes(secret);
        act.Should().NotThrow("the secret must be decodable base32");
    }

    [Fact]
    public void ValidateCode_AcceptsCurrentCodeForGeneratedSecret()
    {
        var secret = _sut.GenerateSecret();
        var currentCode = new Totp(Base32Encoding.ToBytes(secret)).ComputeTotp();

        _sut.ValidateCode(secret, currentCode).Should().BeTrue();
    }

    [Fact]
    public void ValidateCode_RejectsWrongCode()
    {
        var secret = _sut.GenerateSecret();

        _sut.ValidateCode(secret, "000000").Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]    // too short
    [InlineData("1234567")]  // too long
    public void ValidateCode_RejectsMalformedCode(string code)
    {
        var secret = _sut.GenerateSecret();

        _sut.ValidateCode(secret, code).Should().BeFalse();
    }

    [Fact]
    public void GetQrCodeUri_IsWellFormedOtpAuthUri()
    {
        var secret = _sut.GenerateSecret();

        var uri = _sut.GetQrCodeUri(secret, "bob@example.com");

        uri.Should().StartWith("otpauth://totp/");
        uri.Should().Contain("issuer=CommerceSphere");
        uri.Should().Contain("digits=6");
        uri.Should().Contain("period=30");
    }
}
