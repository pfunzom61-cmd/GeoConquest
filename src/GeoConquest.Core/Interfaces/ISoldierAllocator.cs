namespace GeoConquest.Core.Interfaces;

/// <summary>Converts a country's population into its fixed soldier value.</summary>
public interface ISoldierAllocator
{
    /// <summary>Returns the tier value (1 to 9) for the given population. Tier boundaries are inclusive.</summary>
    int GetSoldierValue(long population);
}
