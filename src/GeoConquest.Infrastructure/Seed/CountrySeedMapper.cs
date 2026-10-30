using GeoConquest.Core.Models;

namespace GeoConquest.Infrastructure.Seed;

/// <summary>Turns the rows of the seed file into domain countries, rejecting bad data before it reaches the database.</summary>
public static class CountrySeedMapper
{
    /// <exception cref="InvalidDataException">A row is incomplete or invalid, or an ISO code appears twice. The message names the row.</exception>
    public static IReadOnlyList<Country> ToCountries(CountrySeedFileDto file)
    {
        var entries = file.Countries ?? throw new InvalidDataException("The country seed file has no countries list.");

        var countries = new List<Country>(entries.Count);
        var seenIsoCodes = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < entries.Count; index++)
        {
            var country = ToCountry(entries[index], rowNumber: index + 1);
            if (!seenIsoCodes.Add(country.IsoCode))
            {
                throw new InvalidDataException($"Country seed row {index + 1}: ISO code '{country.IsoCode}' appears more than once.");
            }

            countries.Add(country);
        }

        return countries;
    }

    private static Country ToCountry(CountrySeedEntryDto entry, int rowNumber)
    {
        if (entry.IsoCode is not { Length: 2 } isoCode || !isoCode.All(char.IsAsciiLetterUpper))
        {
            throw new InvalidDataException($"Country seed row {rowNumber}: ISO code must be two uppercase letters, but was '{entry.IsoCode}'.");
        }

        if (string.IsNullOrWhiteSpace(entry.Name))
        {
            throw new InvalidDataException($"Country seed row {rowNumber} ({isoCode}): name is missing.");
        }

        if (entry.Population is not > 0)
        {
            throw new InvalidDataException($"Country seed row {rowNumber} ({isoCode}): population must be positive, but was '{entry.Population}'.");
        }

        return new Country(isoCode, entry.Name, entry.Population.Value);
    }
}
