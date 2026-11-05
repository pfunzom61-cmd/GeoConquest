namespace GeoConquest.Infrastructure.Seed;

/// <summary>One row of <c>countries.json</c>, exactly as it appears in the file. Validated by <see cref="CountrySeedMapper"/>.</summary>
public sealed record CountrySeedEntryDto(string? IsoCode, string? Name, long? Population);
