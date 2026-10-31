using GeoConquest.Core.Models;

namespace GeoConquest.Core.Interfaces;

/// <summary>
/// The one way a game's progress is stored, whoever caused it (the human or the NPC runner): save the game, append
/// its events to the log, then tell the viewers. Keeping it in one place means the ordering and retry rules cannot drift apart.
/// </summary>
public interface IGameProgressRecorder
{
    /// <summary>Stores a brand-new game and its first events.</summary>
    Task RecordNewGameAsync(Game game, IReadOnlyCollection<GameEvent> events, CancellationToken cancellationToken);

    /// <summary>
    /// Saves the game, appends the events and publishes both. The game is saved first because it is the source of
    /// truth and its version check rejects a stale copy. Once it is saved, appending and publishing carry on even if
    /// <paramref name="cancellationToken"/> is cancelled, so a closed request cannot leave events missing from the log.
    /// </summary>
    /// <exception cref="Exceptions.ConcurrencyException">The game was changed elsewhere; nothing was stored or published.</exception>
    Task RecordProgressAsync(Game game, IReadOnlyList<GameEvent> events, CancellationToken cancellationToken);
}
