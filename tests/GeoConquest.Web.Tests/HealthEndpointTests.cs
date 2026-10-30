using System.Net;
using GeoConquest.Web.Tests.Fakes;

namespace GeoConquest.Web.Tests;

public sealed class HealthEndpointTests : IClassFixture<GeoConquestWebFactory>
{
    private readonly GeoConquestWebFactory _factory;

    public HealthEndpointTests(GeoConquestWebFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_WhenRequested_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
