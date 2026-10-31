using GeoConquest.Core.Models;

namespace GeoConquest.Core.Interfaces;

/// <summary>
/// Tells everyone watching a game what just happened. Only public information may leave through here: what the
/// board shows, never the owner's account details. Delivery is best effort, because viewers can always catch up by
/// asking for the saved state, so an implementation must not throw and a failure never undoes a saved move.
/// </summary>
public interface IGameUpdatePublisher
{
    /// <summary>Sends the new events (possibly none) and the game as it now stands, to every viewer of the game.</summary>
    Task PublishAsync(Game game, IReadOnlyList<GameEvent> events, CancellationToken cancellationToken);

    /// <summary>Sends events that did not change the game, such as commentary arriving after its roll.</summary>
    Task PublishEventsAsync(string gameId, IReadOnlyList<GameEvent> events, CancellationToken cancellationToken);
}
