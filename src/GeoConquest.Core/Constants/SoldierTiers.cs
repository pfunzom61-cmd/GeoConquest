namespace GeoConquest.Core.Constants;

/// <summary>The spec's population-to-soldiers table. Upper bounds are inclusive (5,000,000 gives 1 soldier).</summary>
public static class SoldierTiers
{
    private const long Million = 1_000_000;

    /// <summary>Soldiers for any population above the last tier's upper bound.</summary>
    public const int AboveLastTierSoldiers = 9;

    public static readonly IReadOnlyList<(long MaxPopulation, int Soldiers)> Tiers =
    [
        (5 * Million, 1),
        (10 * Million, 2),
        (25 * Million, 3),
        (50 * Million, 4),
        (75 * Million, 5),
        (100 * Million, 6),
        (125 * Million, 7),
        (150 * Million, 8),
    ];
}
