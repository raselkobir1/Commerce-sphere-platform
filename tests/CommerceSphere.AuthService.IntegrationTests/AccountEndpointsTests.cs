using System.Net;
using System.Net.Http.Json;
using CommerceSphere.AuthService.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace CommerceSphere.AuthService.IntegrationTests;

[Collection(AuthApiCollection.Name)]
public class AccountEndpointsTests(AuthApiFactory factory)
{
    private async Task<(string email, HttpClient client)> AuthenticatedClientAsync()
    {
        var client = factory.CreateClient();
        var email = TestHelpers.UniqueEmail();
        var reg = await client.PostJsonAsync("/api/auth/register",
            new { email, password = TestHelpers.ValidPassword, firstName = "Acc", lastName = "Owner" });
        client.SetBearer(reg.Data.GetString("accessToken"));
        return (email, client);
    }

    [Fact]
    public async Task UpdateProfile_ChangesName()
    {
        var (_, client) = await AuthenticatedClientAsync();

        var response = await client.PatchAsync("/api/auth/me",
            JsonContent.Create(new { firstName = "Renamed", lastName = "Person" }));
        var res = await response.ReadApiResultAsync();

        res.Status.Should().Be(HttpStatusCode.OK);
        res.Data.GetString("firstName").Should().Be("Renamed");
    }

    [Fact]
    public async Task ChangePassword_CorrectCurrent_Succeeds_AndNewPasswordWorks()
    {
        var (email, client) = await AuthenticatedClientAsync();

        var change = await client.PostJsonAsync("/api/auth/change-password",
            new { currentPassword = TestHelpers.ValidPassword, newPassword = "Changed1!" });
        change.Status.Should().Be(HttpStatusCode.OK);

        var anon = factory.CreateClient();
        var login = await anon.PostJsonAsync("/api/auth/login", new { email, password = "Changed1!" });
        login.Status.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrent_ReturnsUnauthorized()
    {
        var (_, client) = await AuthenticatedClientAsync();

        var res = await client.PostJsonAsync("/api/auth/change-password",
            new { currentPassword = "NotMyPassword1!", newPassword = "Changed1!" });

        res.Status.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSessions_ReturnsAtLeastOneActiveSession()
    {
        var (_, client) = await AuthenticatedClientAsync();

        var response = await client.GetAsync("/api/auth/sessions");
        var res = await response.ReadApiResultAsync();

        res.Status.Should().Be(HttpStatusCode.OK);
        res.Data.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RevokeAllSessions_Succeeds()
    {
        var (_, client) = await AuthenticatedClientAsync();

        var response = await client.DeleteAsync("/api/auth/sessions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Account_WithoutToken_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auth/sessions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
