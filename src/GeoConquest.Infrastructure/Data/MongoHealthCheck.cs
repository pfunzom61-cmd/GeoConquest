using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace GeoConquest.Infrastructure.Data;

/// <summary>Reports healthy only if MongoDB answers a ping, so <c>/health</c> reflects the real database connection.</summary>
public sealed class MongoHealthCheck(MongoContext mongoContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await mongoContext.Database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception exception) when (exception is MongoException or TimeoutException)
        {
            return HealthCheckResult.Unhealthy("MongoDB did not answer the ping.", exception);
        }
    }
}
