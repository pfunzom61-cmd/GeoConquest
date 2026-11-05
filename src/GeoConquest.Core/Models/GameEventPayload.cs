using GeoConquest.Core.Enums;

namespace GeoConquest.Core.Models;

/// <summary>Structured details of a <see cref="GameEvent"/>. Only the fields relevant to the event type are set.</summary>
public sealed class GameEventPayload
{
    public string? FromCountryCode { get; init; }

    public string? ToCountryCode { get; init; }

    public int? Soldiers { get; init; }

    public IReadOnlyList<int>? AttackerDice { get; init; }

    public IReadOnlyList<int>? DefenderDice { get; init; }

    public int? AttackerLosses { get; init; }

    public int? DefenderLosses { get; init; }

    public int? AttackerSoldiersBefore { get; init; }

    public int? AttackerSoldiersAfter { get; init; }

    public int? DefenderSoldiersBefore { get; init; }

    /// <summary>Soldiers the defender had left after the dice, before any conquest garrison moved in.</summary>
    public int? DefenderSoldiersAfter { get; init; }

    /// <summary>Which roll of the current attack this was, starting at 1.</summary>
    public int? BattleRoundNumber { get; init; }

    /// <summary>The game's round when the dice were rolled.</summary>
    public int? GameRoundNumber { get; init; }

    /// <summary>The attacker's losses across the whole attack so far, including this roll.</summary>
    public int? BattleAttackerLosses { get; init; }

    /// <summary>The defender's losses across the whole attack so far, including this roll.</summary>
    public int? BattleDefenderLosses { get; init; }

    public BattleOutcome? Outcome { get; init; }

    public CommentarySource? CommentarySource { get; init; }
}
