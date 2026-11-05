using GeoConquest.Core.Constants;
using GeoConquest.Core.Enums;
using GeoConquest.Core.Exceptions;
using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;

namespace GeoConquest.Core.Services;

/// <inheritdoc />
public sealed class ReinforcementService : IReinforcementService
{
    public int CalculateReinforcements(int ownedCountryCount, int roundNumber)
    {
        if (roundNumber <= 1 || ownedCountryCount <= 0)
        {
            return 0;
        }

        return ownedCountryCount * GameRules.ReinforcementsPerCountry;
    }

    public void Allocate(Game game, string playerId, IReadOnlyDictionary<string, int> allocations)
    {
        if (game.Phase != TurnPhase.Reinforce)
        {
            throw new GameRuleException("There are no reinforcements to place right now.");
        }

        var ownedTerritories = game.TerritoriesOwnedBy(playerId);

        foreach (var (countryCode, soldiers) in allocations)
        {
            var territory = ownedTerritories.FirstOrDefault(owned => owned.CountryCode == countryCode)
                ?? throw new GameRuleException("Reinforcements can only be placed in countries you own.");

            if (soldiers < 1)
            {
                throw new GameRuleException($"Each country you reinforce must receive at least one soldier ({territory.CountryName}).");
            }
        }

        var allocated = allocations.Values.Sum();
        if (allocated != game.PendingReinforcements)
        {
            throw new GameRuleException(
                $"You must place all {game.PendingReinforcements} reinforcements; you allocated {allocated}.");
        }

        foreach (var (countryCode, soldiers) in allocations)
        {
            game.GetTerritory(countryCode).AddSoldiers(soldiers);
        }

        game.CompleteReinforcement();
    }
}
