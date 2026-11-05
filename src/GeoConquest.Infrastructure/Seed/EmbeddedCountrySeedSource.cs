using System.Text.Json;
using GeoConquest.Core.Models;
using Microsoft.Extensions.Logging;

namespace GeoConquest.Infrastructure.Seed;

/// <summary>
/// Reads <c>countries.json</c> from inside the assembly, so the snapshot ships with the application
/// and does not depend on files being copied next to it.
/// </summary>
public sealed class EmbeddedCountrySeedSource(ILogger<EmbeddedCountrySeedSource> logger) : ICountrySeedSource
{
    private const string ResourceName = "GeoConquest.Infrastructure.Seed.countries.json";

    public async Task<IReadOnlyList<Country>> LoadAsync(CancellationToken cancellationToken)
    {
        await using var stream = typeof(EmbeddedCountrySeedSource).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' was not found.");

        var file = await JsonSerializer.DeserializeAsync<CountrySeedFileDto>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web), cancellationToken)
            ?? throw new InvalidDataException("The country seed file is empty.");

        var countries = CountrySeedMapper.ToCountries(file);
        logger.LogInformation(
            "Loaded {CountryCount} countries from snapshot {SnapshotDate} ({Source})",
            countries.Count, file.SnapshotDate, file.Source);

        return countries;
    }
}
