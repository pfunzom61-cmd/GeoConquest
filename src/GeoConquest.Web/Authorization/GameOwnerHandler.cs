using System.Security.Claims;
using GeoConquest.Core.Models;
using Microsoft.AspNetCore.Authorization;

namespace GeoConquest.Web.Authorization;

/// <summary>Succeeds when the signed-in user's id matches the game's owner. The game is the resource being protected.</summary>
public sealed class GameOwnerHandler : AuthorizationHandler<GameOwnerRequirement, Game>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, GameOwnerRequirement requirement, Game game)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is not null && userId == game.OwnerUserId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
