using GeoConquest.Core.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace GeoConquest.Web.Hubs;

/// <summary>
/// Pushes a game's progress to everyone watching it. Traffic is server to client only: a connection joins the group
/// of the game named in its query string and then just listens. The hub deliberately has no methods a browser can
/// call, so there is nothing for a spectator to invoke. Every action still goes through the authorised JSON endpoints.
/// </summary>
public sealed class GameHub(IGameQueryService gameQueryService) : Hub
{
    /// <summary>The SignalR group that receives everything about one game.</summary>
    public static string GroupName(string gameId) => $"game-{gameId}";

    // Joining here rather than in a client call means an automatic reconnect rejoins the group by itself.
    public override async Task OnConnectedAsync()
    {
        var gameId = Context.GetHttpContext()?.Request.Query["gameId"].ToString();
        if (string.IsNullOrWhiteSpace(gameId) || await gameQueryService.GetGameAsync(gameId, Context.ConnectionAborted) is null)
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(gameId), Context.ConnectionAborted);
        await base.OnConnectedAsync();
    }
}
