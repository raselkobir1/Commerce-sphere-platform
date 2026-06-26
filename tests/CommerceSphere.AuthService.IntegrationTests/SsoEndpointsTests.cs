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
}
