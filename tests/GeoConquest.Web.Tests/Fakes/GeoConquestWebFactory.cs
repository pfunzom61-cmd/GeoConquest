using GeoConquest.Infrastructure.Data;
using GeoConquest.Infrastructure.Identity;
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
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("MongoDb:ConnectionString", "mongodb://unused.invalid:27017");

        builder.ConfigureTestServices(services =>
        {
            services.Remove(services.Single(service => service.ImplementationType == typeof(DatabaseInitializer)));

            services.RemoveAll<IUserStore<ApplicationUser>>();
            services.AddSingleton<IUserStore<ApplicationUser>>(new InMemoryUserStore());

            services.RemoveAll<IRoleStore<ApplicationRole>>();
            services.AddSingleton(Substitute.For<IRoleStore<ApplicationRole>>());
        });
    }
}
