using System.Net;
using CommerceSphere.AuthService.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace CommerceSphere.AuthService.IntegrationTests;

[Collection(AuthApiCollection.Name)]
public class OtpEndpointsTests(AuthApiFactory factory)
{
    private async Task<(string email, HttpClient client)> RegisterAndEnableOtpAsync()
    {
        var client = factory.CreateClient();
        var email = TestHelpers.UniqueEmail("otp");
        var reg = await client.PostJsonAsync("/api/auth/register",
            new { email, password = TestHelpers.ValidPassword, firstName = "One", lastName = "Time" });
        client.SetBearer(reg.Data.GetString("accessToken"));

        var toggle = await client.PostJsonAsync("/api/auth/otp/toggle", new { enable = true });
        toggle.Status.Should().Be(HttpStatusCode.OK);
        return (email, client);
    }

    [Fact]
    public async Task Login_WithOtpEnabled_ReturnsChallenge_AndEmailsCode()
    {
        var (email, _) = await RegisterAndEnableOtpAsync();
        var anon = factory.CreateClient();

        var login = await anon.PostJsonAsync("/api/auth/login", new { email, password = TestHelpers.ValidPassword });

        login.Status.Should().Be(HttpStatusCode.OK);
        login.Data.GetBool("requiresOtp").Should().BeTrue();
        login.Data.GetString("challengeToken").Should().NotBeNullOrWhiteSpace();
        factory.Email.OtpCodes.Should().ContainKey(email.ToLowerInvariant());
    }

    [Fact]
    public async Task Verify_WithEmailedCode_CompletesLogin()
    {
        var (email, _) = await RegisterAndEnableOtpAsync();
        var anon = factory.CreateClient();

        var login = await anon.PostJsonAsync("/api/auth/login", new { email, password = TestHelpers.ValidPassword });
        var challengeToken = login.Data.GetString("challengeToken");
        var code = factory.Email.OtpCodes[email.ToLowerInvariant()];

        var verify = await anon.PostJsonAsync("/api/auth/otp/verify", new { challengeToken, code });

        verify.Status.Should().Be(HttpStatusCode.OK);
        verify.Data.GetString("accessToken").Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Verify_WithWrongCode_ReturnsUnauthorized()
    {
        var (email, _) = await RegisterAndEnableOtpAsync();
        var anon = factory.CreateClient();

        var login = await anon.PostJsonAsync("/api/auth/login", new { email, password = TestHelpers.ValidPassword });
        var challengeToken = login.Data.GetString("challengeToken");

        var verify = await anon.PostJsonAsync("/api/auth/otp/verify",
            new { challengeToken, code = "000000" });

        verify.Status.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Toggle_Disable_RestoresDirectLogin()
    {
        var (email, client) = await RegisterAndEnableOtpAsync();

        var disable = await client.PostJsonAsync("/api/auth/otp/toggle", new { enable = false });
        disable.Status.Should().Be(HttpStatusCode.OK);

        var login = await client.PostJsonAsync("/api/auth/login", new { email, password = TestHelpers.ValidPassword });
        login.Data.GetString("accessToken").Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Toggle_WithoutAuth_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var res = await client.PostJsonAsync("/api/auth/otp/toggle", new { enable = true });

        res.Status.Should().Be(HttpStatusCode.Unauthorized);
    }
}
