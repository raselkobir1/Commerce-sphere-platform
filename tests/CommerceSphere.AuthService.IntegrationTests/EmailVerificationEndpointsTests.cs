using System.Net;
using CommerceSphere.AuthService.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace CommerceSphere.AuthService.IntegrationTests;

[Collection(AuthApiCollection.Name)]
public class EmailVerificationEndpointsTests(AuthApiFactory factory)
{
    [Fact]
    public async Task FullVerificationFlow_ConfirmsEmail()
    {
        var client = factory.CreateClient();
        var email = TestHelpers.UniqueEmail("verify");
        var reg = await client.PostJsonAsync("/api/auth/register",
            new { email, password = TestHelpers.ValidPassword, firstName = "Vee", lastName = "Rify" });

        // Registration emits the first verification token via the fake email service.
        factory.Email.VerificationTokens.TryGetValue(email.ToLowerInvariant(), out var token).Should().BeTrue();

        var confirm = await (await client.GetAsync($"/api/auth/email/verify/confirm?token={token}")).ReadApiResultAsync();
        confirm.Status.Should().Be(HttpStatusCode.OK);

        // Logging in now reports the verified flag.
        var login = await client.PostJsonAsync("/api/auth/login", new { email, password = TestHelpers.ValidPassword });
        login.Data.GetProperty("user").GetBool("emailVerified").Should().BeTrue();
    }

    [Fact]
    public async Task ConfirmVerification_InvalidToken_ReturnsUnprocessable()
    {
        var client = factory.CreateClient();

        var res = await (await client.GetAsync("/api/auth/email/verify/confirm?token=bogus")).ReadApiResultAsync();

        res.Status.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task SendVerification_WhenAlreadyVerified_ReturnsUnprocessable()
    {
        var client = factory.CreateClient();
        var email = TestHelpers.UniqueEmail("verify");
        var reg = await client.PostJsonAsync("/api/auth/register",
            new { email, password = TestHelpers.ValidPassword, firstName = "Vee", lastName = "Rify" });
        client.SetBearer(reg.Data.GetString("accessToken"));

        factory.Email.VerificationTokens.TryGetValue(email.ToLowerInvariant(), out var token);
        await client.GetAsync($"/api/auth/email/verify/confirm?token={token}");

        var res = await client.PostJsonAsync("/api/auth/email/verify/send", new { });

        res.Status.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ResendVerification_UnknownEmail_ReturnsOk_Silently()
    {
        var client = factory.CreateClient();

        var res = await client.PostJsonAsync("/api/auth/email/verify/resend",
            new { email = "nobody@nowhere.dev" });

        res.Status.Should().Be(HttpStatusCode.OK, "resend must not reveal whether an email exists");
    }
}
