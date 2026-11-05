using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;
using GeoConquest.Infrastructure.Seed;
using NSubstitute;

namespace GeoConquest.Infrastructure.Tests.Seed;

public sealed class CountrySeederTests
{
    private readonly ICountryRepository _countries = Substitute.For<ICountryRepository>();
    private readonly ICountrySeedSource _source = Substitute.For<ICountrySeedSource>();

    [Fact]
    public async Task SeedIfEmptyAsync_EmptyCatalogue_InsertsTheSnapshot()
    {
        IReadOnlyList<Country> snapshot = [new Country("ZA", "South Africa", 64_000_000)];
        _countries.CountAsync(Arg.Any<CancellationToken>()).Returns(0L);
        _source.LoadAsync(Arg.Any<CancellationToken>()).Returns(snapshot);

        await new CountrySeeder(_countries, _source).SeedIfEmptyAsync(CancellationToken.None);

        await _countries.Received(1).AddManyAsync(snapshot, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SeedIfEmptyAsync_CatalogueAlreadyFilled_InsertsNothing()
    {
        _countries.CountAsync(Arg.Any<CancellationToken>()).Returns(234L);

        await new CountrySeeder(_countries, _source).SeedIfEmptyAsync(CancellationToken.None);

        await _countries.DidNotReceiveWithAnyArgs().AddManyAsync(default!, default);
    }
}
