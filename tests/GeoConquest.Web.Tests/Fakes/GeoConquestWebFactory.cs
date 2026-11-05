using GeoConquest.Core.Interfaces;
using GeoConquest.Infrastructure.Data;
using GeoConquest.Infrastructure.Identity;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace GeoConquest.Web.Tests.Fakes;

/// <summary>
/// Runs the real application with everything that would reach MongoDB swapped out. The Testing environment also
/// keeps user-secrets from loading (only Development reads them), so a test can never touch the Atlas database.
/// </summary>
public sealed class GeoConquestWebFactory : WebApplicationFactory<Program>
{
    /// <summary>The games the app under test reads and writes, so a test can arrange or inspect them directly.</summary>
    public InMemoryGameRepository Games { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("MongoDb:ConnectionString", "mongodb://unused.invalid:27017");

        // No key means template commentary, so no test can ever call the real model.
        builder.UseSetting("Groq:ApiKey", "");

        // Every test shares one client address, so ordinary tests must not use up the limit between them.
        builder.UseSetting("RateLimiting:Auth:PermitLimit", "1000");

        // NPC turns still run in the background, just without the pause that lets people watch them.
        builder.UseSetting("NpcPacing:NormalDelayMilliseconds", "0");
        builder.UseSetting("NpcPacing:FastForwardDelayMilliseconds", "0");

        builder.ConfigureTestServices(services =>
        {
            services.Remove(services.Single(service => service.ImplementationType == typeof(DatabaseInitializer)));

            services.RemoveAll<IGameRepository>();
            services.AddSingleton<IGameRepository>(Games);
            services.RemoveAll<IGameEventRepository>();
            services.AddSingleton<IGameEventRepository>(new InMemoryGameEventRepository());
            services.RemoveAll<ICountryRepository>();
            services.AddSingleton<ICountryRepository>(new InMemoryCountryRepository());

            services.RemoveAll<INpcStrategy>();
            services.AddSingleton<INpcStrategy, PassiveNpcStrategy>();

            services.RemoveAll<IXmlRepository>();
            services.AddSingleton<IXmlRepository>(new InMemoryXmlRepository());

            services.RemoveAll<IUserStore<ApplicationUser>>();
            services.AddSingleton<IUserStore<ApplicationUser>>(new InMemoryUserStore());

            services.RemoveAll<IRoleStore<ApplicationRole>>();
            services.AddSingleton(Substitute.For<IRoleStore<ApplicationRole>>());
        });
    }
}
