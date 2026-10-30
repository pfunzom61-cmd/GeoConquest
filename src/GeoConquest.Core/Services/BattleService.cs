using GeoConquest.Core.Constants;
using GeoConquest.Core.Enums;
using GeoConquest.Core.Exceptions;
using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;

namespace GeoConquest.Core.Services;

/// <inheritdoc />
public sealed class BattleService(IDiceRoller diceRoller) : IBattleService
{
    public BattleRound RollRound(Game game)
    {
        var battle = game.ActiveBattle
            ?? throw new GameRuleException("There is no attack in progress.");

        var attackingTerritory = game.GetTerritory(battle.AttackerCountryCode);
        var defendingTerritory = game.GetTerritory(battle.DefenderCountryCode);

        if (attackingTerritory.Soldiers < GameRules.MinimumAttackingSoldiers)
        {
            throw new GameRuleException(
                $"{attackingTerritory.CountryName} needs at least {GameRules.MinimumAttackingSoldiers} soldiers to attack.");
        }

        var diceCount = GameRules.DicePerSide(attackingTerritory.Soldiers, defendingTerritory.Soldiers);
        var attackerDice = diceRoller.Roll(diceCount);
        var defenderDice = diceRoller.Roll(diceCount);

        var attackerLosses = 0;
        var defenderLosses = 0;
        for (var pairIndex = 0; pairIndex < diceCount; pairIndex++)
        {
            if (attackerDice[pairIndex] > defenderDice[pairIndex])
            {
                defenderLosses++;
            }
            else if (defenderDice[pairIndex] > attackerDice[pairIndex])
            {
                attackerLosses++;
            }
        }

        if (attackerLosses > 0)
        {
            attackingTerritory.RemoveSoldiers(attackerLosses);
        }

        if (defenderLosses > 0)
        {
            defendingTerritory.RemoveSoldiers(defenderLosses);
        }

        var outcome = BattleOutcome.Continuing;
        var conquestReward = 0;

        if (defendingTerritory.Soldiers == 0)
        {
            outcome = BattleOutcome.Conquered;
            conquestReward = GameRules.ConquestReward(defendingTerritory.SoldierValue);
            defendingTerritory.ChangeOwner(attackingTerritory.OwnerPlayerId, conquestReward);
        }
        else if (attackingTerritory.Soldiers < GameRules.MinimumAttackingSoldiers)
        {
            outcome = BattleOutcome.Failed;
        }

        var round = new BattleRound(attackerDice, defenderDice, attackerLosses, defenderLosses, outcome, conquestReward);
        battle.AddRound(round);
        return round;
    }
}
