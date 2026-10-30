using System.Collections.Concurrent;
using GeoConquest.Core.Constants;
using GeoConquest.Core.Interfaces;

namespace GeoConquest.Core.Services;

/// <summary>
/// Exact dynamic-programming solution over (attacker soldiers, defender soldiers), memoised.
/// A pair of dice is won by either side with probability 15/36 and tied with probability 6/36.
/// A round of k pairs gives a multinomial spread of losses, each leading to a smaller state. The "nobody lost
/// anything" outcome returns to the same state, so it is removed by dividing by (1 - P(all ties)).
/// </summary>
public sealed class BattleOddsCalculator : IBattleOddsCalculator
{
    private const double PairWinProbability = 15.0 / 36.0;
    private const double PairTieProbability = 6.0 / 36.0;

    private readonly ConcurrentDictionary<(int Attackers, int Defenders), double> _cache = new();
    private readonly List<double> _logFactorials = [0.0];
    private readonly object _logFactorialsLock = new();

    public double AttackerWinProbability(int attackerSoldiers, int defenderSoldiers)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(attackerSoldiers);
        ArgumentOutOfRangeException.ThrowIfNegative(defenderSoldiers);

        if (defenderSoldiers == 0)
        {
            return 1.0;
        }

        if (attackerSoldiers < GameRules.MinimumAttackingSoldiers)
        {
            return 0.0;
        }

        var key = (attackerSoldiers, defenderSoldiers);
        if (_cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var probability = Compute(attackerSoldiers, defenderSoldiers);
        _cache[key] = probability;
        return probability;
    }

    private double Compute(int attackerSoldiers, int defenderSoldiers)
    {
        var pairs = GameRules.DicePerSide(attackerSoldiers, defenderSoldiers);
        var logPairFactorial = LogFactorial(pairs);
        var logWin = Math.Log(PairWinProbability);
        var logTie = Math.Log(PairTieProbability);

        var weightedOutcomes = 0.0;

        for (var attackerLosses = 0; attackerLosses <= pairs; attackerLosses++)
        {
            for (var defenderLosses = 0; attackerLosses + defenderLosses <= pairs; defenderLosses++)
            {
                if (attackerLosses == 0 && defenderLosses == 0)
                {
                    continue;
                }

                var ties = pairs - attackerLosses - defenderLosses;
                var logProbability = logPairFactorial
                    - LogFactorial(attackerLosses) - LogFactorial(defenderLosses) - LogFactorial(ties)
                    + ((attackerLosses + defenderLosses) * logWin)
                    + (ties * logTie);

                weightedOutcomes += Math.Exp(logProbability)
                    * AttackerWinProbability(attackerSoldiers - attackerLosses, defenderSoldiers - defenderLosses);
            }
        }

        var probabilityOfNothingChanging = Math.Pow(PairTieProbability, pairs);
        return weightedOutcomes / (1.0 - probabilityOfNothingChanging);
    }

    private double LogFactorial(int value)
    {
        lock (_logFactorialsLock)
        {
            while (_logFactorials.Count <= value)
            {
                _logFactorials.Add(_logFactorials[^1] + Math.Log(_logFactorials.Count));
            }

            return _logFactorials[value];
        }
    }
}
