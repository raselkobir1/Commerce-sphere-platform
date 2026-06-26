using CommerceSphere.AuthService.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace CommerceSphere.AuthService.UnitTests.Domain;

public class RefreshTokenTests
{
    [Fact]
    public void Create_ProducesActiveTokenWithFutureExpiry()
    {
        var userId = Guid.NewGuid();

        var token = RefreshToken.Create(userId, "127.0.0.1");

        token.UserId.Should().Be(userId);
        token.CreatedByIp.Should().Be("127.0.0.1");
        token.Token.Should().NotBeNullOrWhiteSpace();
        token.IsRevoked.Should().BeFalse();
        token.IsExpired.Should().BeFalse();
        token.IsActive.Should().BeTrue();
        token.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void Create_GeneratesUniqueTokensEachTime()
    {
        var a = RefreshToken.Create(Guid.NewGuid(), "ip");
        var b = RefreshToken.Create(Guid.NewGuid(), "ip");

        a.Token.Should().NotBe(b.Token);
    }

    [Fact]
    public void Create_WithNegativeExpiry_IsImmediatelyExpired()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "ip", expiryDays: -1);

        token.IsExpired.Should().BeTrue();
        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Revoke_WithoutReplacement_DeactivatesToken()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "ip");

        token.Revoke();

        token.IsRevoked.Should().BeTrue();
        token.IsActive.Should().BeFalse();
        token.ReplacedByToken.Should().BeNull();
    }

    [Fact]
    public void Revoke_WithReplacement_RecordsRotationChain()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "ip");

        token.Revoke("the-new-token");

        token.IsRevoked.Should().BeTrue();
        token.ReplacedByToken.Should().Be("the-new-token");
    }
}
