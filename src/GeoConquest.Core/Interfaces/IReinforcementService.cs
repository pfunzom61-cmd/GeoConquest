using GeoConquest.Core.Models;

namespace GeoConquest.Core.Interfaces;

/// <summary>Works out and applies the soldiers a player receives at the start of a turn.</summary>
public interface IReinforcementService
{
    /// <summary>
    /// Soldiers owed at the start of a turn: none in round 1, otherwise 2 per controlled country
    /// (so a living player always receives at least 2).
    /// </summary>
    int CalculateReinforcements(int ownedCountryCount, int roundNumber);

    /// <summary>
    /// Places the pending reinforcements. The amounts (keyed by country code) must target countries the player owns,
    /// be positive, and add up exactly to the soldiers owed. Nothing is changed if validation fails.
    /// </summary>
    /// <exception cref="Exceptions.GameRuleException">The allocation breaks one of those rules.</exception>
    void Allocate(Game game, string playerId, IReadOnlyDictionary<string, int> allocations);
}
