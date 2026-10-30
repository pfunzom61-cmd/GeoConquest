using System.Net;
using GeoConquest.Web.Tests.Fakes;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GeoConquest.Web.Tests;

public sealed class HealthEndpointTests : IClassFixture<GeoConquestWebFactory>
{
    private readonly GeoConquestWebFactory _factory;

    public HealthEndpointTests(GeoConquestWebFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData(HealthStatus.Healthy, HttpStatusCode.OK)]
    [InlineData(HealthStatus.Unhealthy, HttpStatusCode.ServiceUnavailable)]
    public async Task Health_ReportsTheDatabaseState(HealthStatus databaseStatus, HttpStatusCode expected)
    {
        var client = _factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                services.Configure<HealthCheckServiceOptions>(options =>
                {
                    options.Registrations.Clear();
                    options.Registrations.Add(new HealthCheckRegistration(
                        "database", _ => new FixedHealthCheck(databaseStatus), failureStatus: null, tags: null));
                })))
            .CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(expected, response.StatusCode);
    }

    private sealed class FixedHealthCheck(HealthStatus status) : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(new HealthCheckResult(status));
    }
}
