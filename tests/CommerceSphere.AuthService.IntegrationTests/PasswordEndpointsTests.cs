using System.Net;
using CommerceSphere.AuthService.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace CommerceSphere.AuthService.IntegrationTests;

[Collection(AuthApiCollection.Name)]
public class PasswordEndpointsTests(AuthApiFactory factory)
{
    private async Task<string> RegisterAsync(string prefix = "pwd")
    {
        var client = factory.CreateClient();
        var email = TestHelpers.UniqueEmail(prefix);
        await client.PostJsonAsync("/api/auth/register",
            new { email, password = TestHelpers.ValidPassword, firstName = "Pass", lastName = "Word" });
        return email;
    }

    [Fact]
    public async Task ForgotThenReset_AllowsLoginWithNewPassword()
    {
        var email = await RegisterAsync();
        var client = factory.CreateClient();

        var forgot = await client.PostJsonAsync("/api/auth/password/forgot", new { email });
        forgot.Status.Should().Be(HttpStatusCode.OK);

        factory.Email.ResetTokens.TryGetValue(email.ToLowerInvariant(), out var token).Should().BeTrue();

        var reset = await client.PostJsonAsync("/api/auth/password/reset",
            new { token, newPassword = "Reset123!" });
        reset.Status.Should().Be(HttpStatusCode.OK);

        var login = await client.PostJsonAsync("/api/auth/login", new { email, password = "Reset123!" });
        login.Status.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ForgotPassword_UnknownEmail_ReturnsOk_Silently()
    {
        var client = factory.CreateClient();

        var res = await client.PostJsonAsync("/api/auth/password/forgot",
            new { email = "ghost@nowhere.dev" });

        res.Status.Should().Be(HttpStatusCode.OK, "forgot-password must not reveal whether an email exists");
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_ReturnsUnprocessable()
    {
        var client = factory.CreateClient();

        var res = await client.PostJsonAsync("/api/auth/password/reset",
            new { token = "invalid-token", newPassword = "Reset123!" });

        res.Status.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}
