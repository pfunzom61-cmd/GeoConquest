using GeoConquest.Core.Enums;

namespace GeoConquest.Core.Models;

/// <summary>The immutable result of one roll of dice within an attack.</summary>
public sealed class BattleRound(
    IReadOnlyList<int> attackerDice,
    IReadOnlyList<int> defenderDice,
    int attackerLosses,
    int defenderLosses,
    BattleOutcome outcome,
    int conquestReward)
{
    /// <summary>Attacker dice in roll order.</summary>
    public IReadOnlyList<int> AttackerDice { get; } = attackerDice;

    /// <summary>Defender dice in roll order.</summary>
    public IReadOnlyList<int> DefenderDice { get; } = defenderDice;

    public int AttackerLosses { get; } = attackerLosses;

    public int DefenderLosses { get; } = defenderLosses;

    public BattleOutcome Outcome { get; } = outcome;

    /// <summary>Soldiers awarded to the conqueror when the outcome is <see cref="BattleOutcome.Conquered"/>; otherwise 0.</summary>
    public int ConquestReward { get; } = conquestReward;
}
