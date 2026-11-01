namespace GeoConquest.Core.Interfaces;

/// <summary>
/// A quick estimate of an attack's chance of conquest, safe to ask for any stack size. The NPC strategy decides with it
/// and the owner's board shows it, so both always quote the same number.
/// </summary>
public interface IAttackOddsEstimator
{
    /// <summary>Probability between 0 and 1 that the attacker conquers the defender, fighting until one side is out.</summary>
    double Estimate(int attackerSoldiers, int defenderSoldiers);
}
