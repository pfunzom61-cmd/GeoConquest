namespace GeoConquest.Core.Interfaces;

/// <summary>The list of games whose NPCs have turns to play. Something else (the background runner) works through it.</summary>
public interface INpcTurnQueue
{
    /// <summary>
    /// Asks for the game's NPC turns to be played. Asking again while the game is already waiting does nothing, and
    /// asking while it is being played makes it run once more afterwards, so a request can never be lost.
    /// </summary>
    void Enqueue(string gameId);
}
