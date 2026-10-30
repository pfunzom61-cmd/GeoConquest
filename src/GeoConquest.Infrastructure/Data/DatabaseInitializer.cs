using GeoConquest.Infrastructure.Seed;
using Microsoft.Extensions.Hosting;

namespace GeoConquest.Infrastructure.Data;

/// <summary>Prepares the database when the application starts: creates indexes, then seeds the country catalogue.</summary>
public sealed class DatabaseInitializer(MongoContext context, CountrySeeder seeder) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await context.EnsureIndexesAsync(cancellationToken);
        await seeder.SeedIfEmptyAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
