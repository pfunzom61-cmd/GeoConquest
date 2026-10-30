using GeoConquest.Core.Constants;
using GeoConquest.Core.Interfaces;

namespace GeoConquest.Core.Services;

/// <summary>Looks a population up in <see cref="SoldierTiers"/>.</summary>
public sealed class SoldierAllocator : ISoldierAllocator
{
    public int GetSoldierValue(long population)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(population);

        foreach (var (maxPopulation, soldiers) in SoldierTiers.Tiers)
        {
            if (population <= maxPopulation)
            {
                return soldiers;
            }
        }

        return SoldierTiers.AboveLastTierSoldiers;
    }
}
