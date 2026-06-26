using System.Net;
using CommerceSphere.AuthService.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace CommerceSphere.AuthService.IntegrationTests;

[Collection(AuthApiCollection.Name)]
public class AuthEndpointsTests(AuthApiFactory factory)
{
    private readonly AuthApiFactory _factory = factory;

    private async Task<(string email, string accessToken, string refreshToken)> RegisterAsync()
    {
        var client = _factory.CreateClient();
        var email = TestHelpers.UniqueEmail();
        var res = await client.PostJsonAsync("/api/auth/register",
            new { email, password = TestHelpers.ValidPassword, firstName = "Test", lastName = "User" });
        res.Status.Should().Be(HttpStatusCode.Created);
        return (email, res.Data.GetString("accessToken"), res.Data.GetString("refreshToken"));
    }

    [Fact]
    public async Task Register_ReturnsCreated_WithUnverifiedUserAndTokens()
    {
        var client = _factory.CreateClient();
        var email = TestHelpers.UniqueEmail();

        var res = await client.PostJsonAsync("/api/auth/register",
            new { email, password = TestHelpers.ValidPassword, firstName = "Jane", lastName = "Doe" });

        res.Status.Should().Be(HttpStatusCode.Created);
        res.Data.GetString("accessToken").Should().NotBeNullOrWhiteSpace();
        res.Data.GetProperty("user").GetString("email").Should().Be(email.ToLowerInvariant());
        res.Data.GetProperty("user").GetBool("emailVerified").Should().BeFalse();
        _factory.Email.VerificationTokens.Should().ContainKey(email.ToLowerInvariant());
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var (email, _, _) = await RegisterAsync();
        var client = _factory.CreateClient();

        var res = await client.PostJsonAsync("/api/auth/register",
            new { email, password = TestHelpers.ValidPassword, firstName = "Dup", lastName = "User" });

        res.Status.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WeakPassword_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var res = await client.PostJsonAsync("/api/auth/register",
            new { email = TestHelpers.UniqueEmail(), password = "weak", firstName = "A", lastName = "B" });

        res.Status.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokens()
    {
        var (email, _, _) = await RegisterAsync();
        var client = _factory.CreateClient();

        var res = await client.PostJsonAsync("/api/auth/login",
            new { email, password = TestHelpers.ValidPassword });

        res.Status.Should().Be(HttpStatusCode.OK);
        res.Data.GetString("accessToken").Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var (email, _, _) = await RegisterAsync();
        var client = _factory.CreateClient();

        var res = await client.PostJsonAsync("/api/auth/login",
            new { email, password = "WrongPass1!" });

        res.Status.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_UnknownEmail_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var res = await client.PostJsonAsync("/api/auth/login",
            new { email = TestHelpers.UniqueEmail(), password = TestHelpers.ValidPassword });

        res.Status.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_WithToken_ReturnsCurrentUser()
    {
        var (email, accessToken, _) = await RegisterAsync();
        var client = _factory.CreateClient();
        client.SetBearer(accessToken);

        var res = await (await client.GetAsync("/api/auth/me")).ReadApiResultAsync();

        res.Status.Should().Be(HttpStatusCode.OK);
        res.Data.GetString("email").Should().Be(email.ToLowerInvariant());
    }

    [Fact]
    public async Task GetMe_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_RotatesTokens()
    {
        var (_, _, refreshToken) = await RegisterAsync();
        var client = _factory.CreateClient();

        var res = await client.PostJsonAsync("/api/auth/refresh-token", new { refreshToken });

        res.Status.Should().Be(HttpStatusCode.OK);
        res.Data.GetString("refreshToken").Should().NotBe(refreshToken);
    }

    [Fact]
    public async Task RevokeToken_ThenReuse_IsRejected()
    {
        var (_, accessToken, refreshToken) = await RegisterAsync();
        var client = _factory.CreateClient();
        client.SetBearer(accessToken);

        var revoke = await client.PostJsonAsync("/api/auth/revoke-token", new { refreshToken });
        revoke.Status.Should().Be(HttpStatusCode.OK);

        client.ClearBearer();
        var reuse = await client.PostJsonAsync("/api/auth/refresh-token", new { refreshToken });
        reuse.Status.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RevokeToken_WithoutAuth_ReturnsUnauthorized()
    {
        var (_, _, refreshToken) = await RegisterAsync();
        var client = _factory.CreateClient();

        var res = await client.PostJsonAsync("/api/auth/revoke-token", new { refreshToken });

        res.Status.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUsers_AsCustomer_ReturnsForbidden()
    {
        var (_, accessToken, _) = await RegisterAsync();
        var client = _factory.CreateClient();
        client.SetBearer(accessToken);

        var response = await client.GetAsync("/api/auth/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
