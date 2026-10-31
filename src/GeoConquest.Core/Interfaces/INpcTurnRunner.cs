using GeoConquest.Core.Models;

namespace GeoConquest.Core.Interfaces;

/// <summary>Plays the turn of the NPC whose turn it currently is, using the same engine as the human.</summary>
public interface INpcTurnRunner
{
    /// <summary>
    /// Plays the next step of the current NPC's turn (for example one set of reinforcements, or one round of dice) and
    /// returns the events it produced. The step is chosen from the game's turn phase alone, so calling this repeatedly
    /// plays the whole turn, even if the game was saved and reloaded in between. The last step ends the NPC's turn.
    /// </summary>
    /// <exception cref="Exceptions.GameRuleException">The game is over or it is the human's turn.</exception>
    IReadOnlyList<GameEvent> PlayStep(Game game);
}
