using GeoConquest.Core.Models;

namespace GeoConquest.Core.Interfaces;

/// <summary>Plays one turn on behalf of the NPC whose turn it currently is, using the same engine as the human.</summary>
public interface INpcTurnRunner
{
    /// <summary>Plays the current NPC's whole turn and returns the events it produced. Always ends that NPC's turn.</summary>
    IReadOnlyList<GameEvent> PlayTurn(Game game);
}
