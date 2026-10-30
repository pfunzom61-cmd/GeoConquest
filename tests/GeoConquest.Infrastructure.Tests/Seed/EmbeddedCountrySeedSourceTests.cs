using GeoConquest.Infrastructure.Seed;
using Microsoft.Extensions.Logging.Abstractions;

namespace GeoConquest.Infrastructure.Tests.Seed;

public sealed class EmbeddedCountrySeedSourceTests
{
    [Fact]
    public async Task LoadAsync_ShippedSnapshot_HasEnoughValidCountriesForAGame()
    {
        var source = new EmbeddedCountrySeedSource(NullLogger<EmbeddedCountrySeedSource>.Instance);

        var countries = await source.LoadAsync(CancellationToken.None);

        Assert.True(countries.Count >= 10, "A game needs ten distinct countries.");
        Assert.Contains(countries, country => country.IsoCode == "ZA" && country.Name == "South Africa" && country.Population > 5_000_000);
    }
}
