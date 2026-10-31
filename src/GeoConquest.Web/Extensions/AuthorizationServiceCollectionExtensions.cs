using GeoConquest.Web.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace GeoConquest.Web.Extensions;

/// <summary>Registers the game authorization policy and its handler.</summary>
public static class AuthorizationServiceCollectionExtensions
{
    public static IServiceCollection AddGeoConquestAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(GamePolicies.CanPlayGame, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new GameOwnerRequirement()));

        services.AddSingleton<IAuthorizationHandler, GameOwnerHandler>();
        return services;
    }
}
