using GeoConquest.Core.Interfaces;
using GeoConquest.Infrastructure.Configuration;
using GeoConquest.Infrastructure.Data;
using GeoConquest.Infrastructure.Data.Mapping;
using GeoConquest.Infrastructure.Repositories;
using GeoConquest.Infrastructure.Seed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace GeoConquest.Infrastructure;

/// <summary>Registers everything that talks to the outside world.</summary>
public static class DependencyInjection
{
    // Fail quickly rather than hang a request or a health check when the database is unreachable.
    private static readonly TimeSpan ServerSelectionTimeout = TimeSpan.FromSeconds(5);

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        MongoMappingConfiguration.Register();

        services.AddOptions<MongoDbOptions>()
            .Bind(configuration.GetSection(MongoDbOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // MongoClient is thread-safe and pools connections, so the whole application shares one instance.
        services.AddSingleton<IMongoClient>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<MongoDbOptions>>().Value;
            var settings = MongoClientSettings.FromConnectionString(options.ConnectionString);
            settings.ServerSelectionTimeout = ServerSelectionTimeout;
            return new MongoClient(settings);
        });
        services.AddSingleton<MongoContext>();

        services.AddSingleton<ICountryRepository, MongoCountryRepository>();
        services.AddSingleton<IGameRepository, MongoGameRepository>();
        services.AddSingleton<IGameEventRepository, MongoGameEventRepository>();

        services.AddSingleton<ICountrySeedSource, EmbeddedCountrySeedSource>();
        services.AddSingleton<CountrySeeder>();
        services.AddHostedService<DatabaseInitializer>();

        services.AddHealthChecks().AddCheck<MongoHealthCheck>("mongodb");

        return services;
    }
}
