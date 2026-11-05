using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;
using GeoConquest.Web.ViewModels;
using Microsoft.AspNetCore.SignalR;

namespace GeoConquest.Web.Hubs;

/// <summary>
/// Sends game updates to the SignalR group of the game. It builds the same public view models the pages use, in their
/// spectator form (no actions, no account details), so a viewer's data is identical whoever is watching.
/// </summary>
public sealed class SignalRGameUpdatePublisher(IHubContext<GameHub> hubContext, ILogger<SignalRGameUpdatePublisher> logger) : IGameUpdatePublisher
{
    public async Task PublishAsync(Game game, IReadOnlyList<GameEvent> events, CancellationToken cancellationToken)
    {
        try
        {
            var viewers = hubContext.Clients.Group(GameHub.GroupName(game.Id));

            // Events go first so a viewer never sees a state that mentions something its log has not shown yet.
            if (events.Count > 0)
            {
                await viewers.SendAsync(GameHubMessages.EventsAppended, events.Select(GameEventViewModel.From).ToList(), cancellationToken);
            }

            await viewers.SendAsync(GameHubMessages.StateUpdated, GameStateViewModel.From(game, viewerIsOwner: false), cancellationToken);
        }
        catch (Exception exception)
        {
            // Viewers recover by syncing, so a failed broadcast must never fail the move that caused it.
            logger.LogWarning(exception, "Could not broadcast an update for game {GameId}.", game.Id);
        }
    }

    public async Task PublishEventsAsync(string gameId, IReadOnlyList<GameEvent> events, CancellationToken cancellationToken)
    {
        try
        {
            await hubContext.Clients.Group(GameHub.GroupName(gameId))
                .SendAsync(GameHubMessages.EventsAppended, events.Select(GameEventViewModel.From).ToList(), cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not broadcast events for game {GameId}.", gameId);
        }
    }
}
