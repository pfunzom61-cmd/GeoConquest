namespace GeoConquest.Core.Models;

/// <summary>A country from the world catalogue that can be picked for a game.</summary>
public sealed class Country
{
    public Country(string isoCode, string name, long population)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(population);

        IsoCode = isoCode;
        Name = name;
        Population = population;
    }

    public string IsoCode { get; }

    public string Name { get; }

    public long Population { get; }
}
