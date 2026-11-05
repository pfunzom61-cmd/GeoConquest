using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;
using GeoConquest.Core.Services;
using GeoConquest.Infrastructure.AI;
using GeoConquest.Infrastructure.Configuration;
using GeoConquest.Infrastructure.Data;
using GeoConquest.Infrastructure.Data.Mapping;
using GeoConquest.Infrastructure.Repositories;
using GeoConquest.Infrastructure.Seed;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace GeoConquest.Infrastructure;

/// <summary>Registers everything that talks to the outside world.</summary>
public static class DependencyInjection
{
    // Fail quickly rather than hang a request or a health check when the database is unreachable.
    private static readonly TimeSpan ServerSelectionTimeout = TimeSpan.FromSeconds(5);

    private static readonly TimeSpan HttpConnectionLifetime = TimeSpan.FromMinutes(5);

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

        // The application name keeps the keys valid across hosts and redeploys.
        services.AddDataProtection().SetApplicationName("GeoConquest");
        services.AddSingleton<IXmlRepository, MongoDataProtectionKeyRepository>();
        services.AddSingleton<IConfigureOptions<KeyManagementOptions>, MongoDataProtectionConfiguration>();

        services.AddSingleton<ICountryRepository, MongoCountryRepository>();
        services.AddSingleton<IGameRepository, MongoGameRepository>();
        services.AddSingleton<IGameEventRepository, MongoGameEventRepository>();

        services.AddSingleton<ICountrySeedSource, EmbeddedCountrySeedSource>();
        services.AddSingleton<CountrySeeder>();
        services.AddHostedService<DatabaseInitializer>();

        services.AddHealthChecks().AddCheck<MongoHealthCheck>("mongodb");

        AddCommentary(services, configuration);

        return services;
    }

    private static void AddCommentary(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<GroqOptions>().Bind(configuration.GetSection(GroqOptions.SectionName));
        services.AddOptions<CommentaryOptions>().Bind(configuration.GetSection(CommentaryOptions.SectionName));
        // Core cannot depend on IOptions, so it receives the bound values themselves.
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<CommentaryOptions>>().Value);

        services.AddSingleton<TemplateCommentaryService>();
        services.AddSingleton<CommentaryRateLimiter>();

        // The commentary service is a singleton and so keeps its client for good. Recycling connections in the handler,
        // instead of the factory replacing handlers, is how such a client still notices DNS changes.
        // The factory's built-in request logging is removed so that no header can ever reach a log.
        services.AddHttpClient<GroqCommentaryService>((provider, client) =>
            {
                var baseUrl = provider.GetRequiredService<IOptions<GroqOptions>>().Value.BaseUrl;
                client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { PooledConnectionLifetime = HttpConnectionLifetime })
            .SetHandlerLifetime(Timeout.InfiniteTimeSpan)
            .RemoveAllLoggers();

        services.AddSingleton<ICommentaryService>(provider =>
        {
            var template = provider.GetRequiredService<TemplateCommentaryService>();

            if (!provider.GetRequiredService<IOptions<GroqOptions>>().Value.HasApiKey)
            {
                provider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DependencyInjection))
                    .LogInformation("Groq:ApiKey is not set, so battle commentary will use templates.");
                return template;
            }

            return new ResilientCommentaryService(
                provider.GetRequiredService<GroqCommentaryService>(),
                template,
                provider.GetRequiredService<CommentaryRateLimiter>(),
                provider.GetRequiredService<CommentaryOptions>(),
                provider.GetRequiredService<TimeProvider>());
        });
    }
}
