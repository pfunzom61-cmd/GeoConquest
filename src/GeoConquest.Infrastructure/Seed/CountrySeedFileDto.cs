namespace GeoConquest.Infrastructure.Seed;

/// <summary>The shape of <c>countries.json</c>: a dated population snapshot and where it came from.</summary>
public sealed record CountrySeedFileDto(string? Source, string? SnapshotDate, IReadOnlyList<CountrySeedEntryDto>? Countries);
