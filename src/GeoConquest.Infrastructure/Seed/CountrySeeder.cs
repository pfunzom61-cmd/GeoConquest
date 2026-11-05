using GeoConquest.Core.Interfaces;

namespace GeoConquest.Infrastructure.Seed;

/// <summary>Fills the country catalogue the first time the application meets an empty database.</summary>
public sealed class CountrySeeder(ICountryRepository countries, ICountrySeedSource source)
{
    public async Task SeedIfEmptyAsync(CancellationToken cancellationToken)
    {
        if (await countries.CountAsync(cancellationToken) > 0)
        {
            return;
        }

        var seed = await source.LoadAsync(cancellationToken);
        await countries.AddManyAsync(seed, cancellationToken);
    }
}
