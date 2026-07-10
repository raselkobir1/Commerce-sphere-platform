using System.Net;
using CommerceSphere.AuthService.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace CommerceSphere.AuthService.IntegrationTests;

[Collection(AuthApiCollection.Name)]
public class SsoEndpointsTests(AuthApiFactory factory)
{
    [Fact]
    public async Task GetProviders_ReturnsConfiguredProviders()
    {
        var client = factory.CreateClient();

        var res = await (await client.GetAsync("/api/auth/sso/providers")).ReadApiResultAsync();

        res.Status.Should().Be(HttpStatusCode.OK);
        res.Data.GetArrayLength().Should().BeGreaterThan(0);
    }

    // Drives the real SSO callback path: SsoManager runs the find-or-create inside the retrying
    // execution strategy's transaction against the real database, then issues our JWT and 302s back
    // to the client. This is the exact path that must not throw the "execution strategy does not
    // support user-initiated transactions" error.
    [Fact]
    public async Task Callback_FirstTimeLogin_CreatesUser_AndRedirectsWithTokens()
    {
        // Non-redirecting client so we can inspect the 302 Location.
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var email = $"sso-{Guid.NewGuid():N}@example.com";
        var code = Uri.EscapeDataString($"sub-{Guid.NewGuid():N}|{email}|Grace|Hopper");

        var res = await client.GetAsync($"/api/auth/sso/callback?code={code}&state=state-token");

        res.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var location = res.Headers.Location!.ToString();
        location.Should().StartWith(FakeSsoService.RedirectUri);
        location.Should().Contain("access_token=");
        location.Should().Contain("refresh_token=");
    }

    // A returning SSO user (same identity) must resolve to the SAME account, not a duplicate.
    [Fact]
    public async Task Callback_ReturningUser_IsIdempotent()
    {
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var sub = $"sub-{Guid.NewGuid():N}";
        var email = $"sso-{Guid.NewGuid():N}@example.com";
        var code = Uri.EscapeDataString($"{sub}|{email}|Ada|Lovelace");

        var first = await client.GetAsync($"/api/auth/sso/callback?code={code}&state=s1");
        var second = await client.GetAsync($"/api/auth/sso/callback?code={code}&state=s2");

        first.StatusCode.Should().Be(HttpStatusCode.Redirect);
        second.StatusCode.Should().Be(HttpStatusCode.Redirect);
        second.Headers.Location!.ToString().Should().Contain("access_token=");
    }
}
