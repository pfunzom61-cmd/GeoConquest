namespace GeoConquest.Core.Interfaces;

/// <summary>Computes the exact chance that an attack succeeds. Used by NPC decisions and UI hints.</summary>
public interface IBattleOddsCalculator
{
    /// <summary>
    /// Probability that an attacker with <paramref name="attackerSoldiers"/> soldiers conquers a defender with
    /// <paramref name="defenderSoldiers"/>, fighting every round until one side is out (the defender at 0, or the
    /// attacker down to its last soldier).
    /// </summary>
    double AttackerWinProbability(int attackerSoldiers, int defenderSoldiers);
}
