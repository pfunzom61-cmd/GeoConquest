namespace GeoConquest.Core.Interfaces;

/// <summary>Plays a game's NPC turns in the background, one saved and published step at a time.</summary>
public interface INpcTurnProcessor
{
    /// <summary>
    /// Repeats: reload the game, play one NPC step, save it, append its events, publish it, then wait (the normal or
    /// fast-forward delay). Stops when the game is over, missing, or it is the human's turn. Safe to call for a game
    /// that is not waiting on an NPC, and safe to call again after a restart, because every step is saved before the
    /// next is chosen.
    /// </summary>
    /// <exception cref="Exceptions.ConcurrencyException">The game kept being changed elsewhere; the caller may try again.</exception>
    Task PlayUntilHumanTurnAsync(string gameId, CancellationToken cancellationToken);
}
