using GeoConquest.Core.Enums;

namespace GeoConquest.Core.Models;

/// <summary>
/// The facts of one dice round, which is all that commentary is written from. It deliberately has no player names:
/// country names come from the catalogue and everything else is a number, so nothing a user typed can reach the model.
/// </summary>
public sealed record BattleContext(
    string AttackerCountryName,
    string DefenderCountryName,
    int AttackerSoldiersBefore,
    int AttackerSoldiersAfter,
    int DefenderSoldiersBefore,
    int DefenderSoldiersAfter,
    IReadOnlyList<int> AttackerDice,
    IReadOnlyList<int> DefenderDice,
    int AttackerLosses,
    int DefenderLosses,
    int BattleRoundNumber,
    int GameRoundNumber,
    int BattleAttackerLosses,
    int BattleDefenderLosses,
    BattleOutcome Outcome)
{
    public BattleMomentum Momentum => BattleAttackerLosses.CompareTo(BattleDefenderLosses) switch
    {
        < 0 => BattleMomentum.AttackerAhead,
        > 0 => BattleMomentum.DefenderAhead,
        _ => BattleMomentum.Even,
    };

    /// <summary>Reads the facts from a dice event. The game only supplies the names of the two countries.</summary>
    public static BattleContext FromRoll(Game game, GameEvent diceRolled)
    {
        var payload = diceRolled.Payload
            ?? throw new ArgumentException("A dice event must carry the facts of the round.", nameof(diceRolled));

        return new BattleContext(
            game.GetTerritory(payload.FromCountryCode!).CountryName,
            game.GetTerritory(payload.ToCountryCode!).CountryName,
            payload.AttackerSoldiersBefore!.Value,
            payload.AttackerSoldiersAfter!.Value,
            payload.DefenderSoldiersBefore!.Value,
            payload.DefenderSoldiersAfter!.Value,
            payload.AttackerDice!,
            payload.DefenderDice!,
            payload.AttackerLosses!.Value,
            payload.DefenderLosses!.Value,
            payload.BattleRoundNumber!.Value,
            payload.GameRoundNumber!.Value,
            payload.BattleAttackerLosses!.Value,
            payload.BattleDefenderLosses!.Value,
            payload.Outcome!.Value);
    }
}
