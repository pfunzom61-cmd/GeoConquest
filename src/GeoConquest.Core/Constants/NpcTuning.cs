namespace GeoConquest.Core.Constants;

/// <summary>The NPC strategy's tunable numbers, kept in one place so its behaviour can be adjusted without touching the logic.</summary>
public static class NpcTuning
{
    /// <summary>An NPC only starts an attack whose chance of conquest is at least this.</summary>
    public const double AttackThreshold = 0.60;

    /// <summary>An NPC calls off an attack in progress once its chance of conquest falls below this.</summary>
    public const double StopThreshold = 0.35;

    /// <summary>A country is "at risk" when the strongest enemy stack would take it with at least this chance.</summary>
    public const double ThreatThreshold = 0.70;

    /// <summary>Largest share of a turn's reinforcements spent shoring up threatened countries; the rest backs the attack base.</summary>
    public const double DefenceShare = 0.5;

    /// <summary>
    /// Added to a target's reward when its owner leads the game. Kept small: a large bonus makes every NPC gang up on
    /// whoever leads, and in simulation some games then never finished.
    /// </summary>
    public const double LeaderBonus = 0.5;

    /// <summary>Added to a target's reward when taking it eliminates its owner.</summary>
    public const double EliminationBonus = 1.0;

    /// <summary>NPCs may not attack the human up to and including this round (assessment assumption 11).</summary>
    public const int HumanProtectedThroughRound = 1;

    /// <summary>
    /// Largest stack size passed to the odds calculator. The calculation is exact but its cost grows steeply with
    /// stack size (240 v 240 took about 30 seconds), and beyond this size the result is settled by the ratio between the
    /// stacks. Larger pairs are scaled down together, which keeps that ratio.
    /// </summary>
    public const int MaxOddsStackSize = 60;
}
