using GeoConquest.Core.Enums;
using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;

namespace GeoConquest.Core.Services;

/// <summary>
/// Temporary NPC for milestone 3: places any reinforcements on its first country and skips, so the turn loop can be
/// played end to end. Milestone 4 replaces it with the real strategy-driven runner.
/// </summary>
public sealed class SkippingNpcTurnRunner(IGameEngine gameEngine) : INpcTurnRunner
{
    public IReadOnlyList<GameEvent> PlayTurn(Game game)
    {
        var npc = game.CurrentPlayer;
        var events = new List<GameEvent>();

        if (game.Phase == TurnPhase.Reinforce)
        {
            var firstCountry = game.TerritoriesOwnedBy(npc.Id)[0];
            var allocation = new Dictionary<string, int> { [firstCountry.CountryCode] = game.PendingReinforcements };
            events.AddRange(gameEngine.AllocateReinforcements(game, npc.Id, allocation));
        }

        events.AddRange(gameEngine.Skip(game, npc.Id));
        return events;
    }
}
