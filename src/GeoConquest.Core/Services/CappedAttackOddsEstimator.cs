using GeoConquest.Core.Constants;
using GeoConquest.Core.Interfaces;

namespace GeoConquest.Core.Services;

/// <summary>
/// Asks the exact odds calculator, but shrinks large stacks first. The exact calculation grows steeply with stack size,
/// and beyond <see cref="NpcTuning.MaxOddsStackSize"/> the result is settled by the ratio between the stacks, which
/// scaling both together keeps (capping each separately would turn 200 v 100 into an even 60 v 60).
/// </summary>
public sealed class CappedAttackOddsEstimator(IBattleOddsCalculator oddsCalculator) : IAttackOddsEstimator
{
    public double Estimate(int attackerSoldiers, int defenderSoldiers)
    {
        var largest = Math.Max(attackerSoldiers, defenderSoldiers);
        if (largest > NpcTuning.MaxOddsStackSize)
        {
            var scale = (double)NpcTuning.MaxOddsStackSize / largest;
            attackerSoldiers = ScaleDown(attackerSoldiers, scale);
            defenderSoldiers = ScaleDown(defenderSoldiers, scale);
        }

        return oddsCalculator.AttackerWinProbability(attackerSoldiers, defenderSoldiers);
    }

    /// <summary>Shrinks a stack in proportion, never below the 2 soldiers that still allow an attack (a stack of 0 or 1 is left alone).</summary>
    private static int ScaleDown(int soldiers, double scale) =>
        soldiers <= 1 ? soldiers : Math.Max(GameRules.MinimumAttackingSoldiers, (int)Math.Round(soldiers * scale));
}
