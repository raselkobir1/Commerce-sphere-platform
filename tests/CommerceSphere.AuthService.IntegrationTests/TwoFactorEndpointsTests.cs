using System.Net;
using CommerceSphere.AuthService.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace CommerceSphere.AuthService.IntegrationTests;

[Collection(AuthApiCollection.Name)]
public class TwoFactorEndpointsTests(AuthApiFactory factory)
{
    // Registers a user, enables + confirms 2FA, and returns (email, secret) for challenge tests.
    private async Task<(string email, string secret)> EnrollTwoFactorAsync()
    {
        var client = factory.CreateClient();
        var email = TestHelpers.UniqueEmail("2fa");
        var reg = await client.PostJsonAsync("/api/auth/register",
            new { email, password = TestHelpers.ValidPassword, firstName = "Two", lastName = "Factor" });
        client.SetBearer(reg.Data.GetString("accessToken"));

        var setup = await client.PostJsonAsync("/api/auth/2fa/setup", new { });
        setup.Status.Should().Be(HttpStatusCode.OK);
        var secret = setup.Data.GetString("secretKey");

        var confirm = await client.PostJsonAsync("/api/auth/2fa/confirm",
            new { code = TestHelpers.ComputeTotp(secret) });
        confirm.Status.Should().Be(HttpStatusCode.OK);
        confirm.Data.GetProperty("user").GetBool("isActiveTwoFactor").Should().BeTrue();

        return (email, secret);
    }

    [Fact]
    public async Task Setup_ReturnsSecretAndQrCode()
    {
        var client = factory.CreateClient();
        var reg = await client.PostJsonAsync("/api/auth/register",
            new { email = TestHelpers.UniqueEmail("2fa"), password = TestHelpers.ValidPassword, firstName = "Two", lastName = "Factor" });
        client.SetBearer(reg.Data.GetString("accessToken"));

        var setup = await client.PostJsonAsync("/api/auth/2fa/setup", new { });

        setup.Status.Should().Be(HttpStatusCode.OK);
        setup.Data.GetString("secretKey").Should().NotBeNullOrWhiteSpace();
        setup.Data.GetString("qrCodeUri").Should().StartWith("otpauth://");
    }

    [Fact]
    public async Task Confirm_WithInvalidCode_ReturnsUnprocessable()
    {
        var client = factory.CreateClient();
        var reg = await client.PostJsonAsync("/api/auth/register",
            new { email = TestHelpers.UniqueEmail("2fa"), password = TestHelpers.ValidPassword, firstName = "Two", lastName = "Factor" });
        client.SetBearer(reg.Data.GetString("accessToken"));
        await client.PostJsonAsync("/api/auth/2fa/setup", new { });

        var confirm = await client.PostJsonAsync("/api/auth/2fa/confirm", new { code = "000000" });

        confirm.Status.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Login_With2FAEnabled_ReturnsChallengeInsteadOfTokens()
    {
        var (email, _) = await EnrollTwoFactorAsync();
        var client = factory.CreateClient();

        var login = await client.PostJsonAsync("/api/auth/login", new { email, password = TestHelpers.ValidPassword });

        login.Status.Should().Be(HttpStatusCode.OK);
        login.Data.GetBool("requiresTwoFactor").Should().BeTrue();
        login.Data.GetString("challengeToken").Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Verify_WithValidCode_CompletesLogin()
    {
        var (email, secret) = await EnrollTwoFactorAsync();
        var client = factory.CreateClient();

        var login = await client.PostJsonAsync("/api/auth/login", new { email, password = TestHelpers.ValidPassword });
        var challengeToken = login.Data.GetString("challengeToken");

        var verify = await client.PostJsonAsync("/api/auth/2fa/verify",
            new { challengeToken, code = TestHelpers.ComputeTotp(secret) });

        verify.Status.Should().Be(HttpStatusCode.OK);
        verify.Data.GetString("accessToken").Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Verify_WithBadChallengeToken_ReturnsUnauthorized()
    {
        var (_, secret) = await EnrollTwoFactorAsync();
        var client = factory.CreateClient();

        var verify = await client.PostJsonAsync("/api/auth/2fa/verify",
            new { challengeToken = "not-a-real-token", code = TestHelpers.ComputeTotp(secret) });

        verify.Status.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Disable_WithValidCode_TurnsOffTwoFactor()
    {
        var (email, secret) = await EnrollTwoFactorAsync();
        // Re-authenticate through the 2FA challenge to obtain a usable access token.
        var client = factory.CreateClient();
        var login = await client.PostJsonAsync("/api/auth/login", new { email, password = TestHelpers.ValidPassword });
        var verify = await client.PostJsonAsync("/api/auth/2fa/verify",
            new { challengeToken = login.Data.GetString("challengeToken"), code = TestHelpers.ComputeTotp(secret) });
        client.SetBearer(verify.Data.GetString("accessToken"));

        var disable = await client.PostJsonAsync("/api/auth/2fa/disable",
            new { code = TestHelpers.ComputeTotp(secret) });
        disable.Status.Should().Be(HttpStatusCode.OK);

        // Login now issues tokens directly — no challenge.
        var plainLogin = await client.PostJsonAsync("/api/auth/login", new { email, password = TestHelpers.ValidPassword });
        plainLogin.Data.GetString("accessToken").Should().NotBeNullOrWhiteSpace();
    }
}
