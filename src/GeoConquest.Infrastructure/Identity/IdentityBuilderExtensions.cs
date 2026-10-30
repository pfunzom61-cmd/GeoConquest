using AspNetCore.Identity.MongoDbCore;
using GeoConquest.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MongoDbGenericRepository;

namespace GeoConquest.Infrastructure.Identity;

/// <summary>Connects ASP.NET Core Identity to MongoDB.</summary>
public static class IdentityBuilderExtensions
{
    /// <summary>
    /// Stores users and roles in MongoDB through the application's single client. The package's own
    /// <c>AddMongoDbStores</c> needs its context before the container exists, which would force a second client.
    /// </summary>
    public static IdentityBuilder AddMongoIdentityStores(this IdentityBuilder builder)
    {
        builder.Services.AddSingleton<IMongoDbContext>(provider =>
            new MongoDbContext(provider.GetRequiredService<MongoContext>().Database));

        builder.Services.AddScoped<IUserStore<ApplicationUser>>(provider =>
            new MongoUserStore<ApplicationUser, ApplicationRole, IMongoDbContext, Guid>(
                provider.GetRequiredService<IMongoDbContext>(),
                provider.GetRequiredService<IdentityErrorDescriber>()));

        builder.Services.AddScoped<IRoleStore<ApplicationRole>>(provider =>
            new MongoRoleStore<ApplicationRole, IMongoDbContext, Guid>(
                provider.GetRequiredService<IMongoDbContext>(),
                provider.GetRequiredService<IdentityErrorDescriber>()));

        return builder;
    }
}
