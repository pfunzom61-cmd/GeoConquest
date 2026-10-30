using GeoConquest.Infrastructure.Seed;

namespace GeoConquest.Infrastructure.Tests.Seed;

public sealed class CountrySeedMapperTests
{
    [Fact]
    public void ToCountries_ValidRow_MapsIsoCodeNameAndPopulation()
    {
        var file = FileWith(new CountrySeedEntryDto("ZA", "South Africa", 64_000_000));

        var country = Assert.Single(CountrySeedMapper.ToCountries(file));

        Assert.Equal("ZA", country.IsoCode);
        Assert.Equal("South Africa", country.Name);
        Assert.Equal(64_000_000, country.Population);
    }

    [Theory]
    [InlineData("za", "South Africa", 1000L)]
    [InlineData("ZAF", "South Africa", 1000L)]
    [InlineData(null, "South Africa", 1000L)]
    [InlineData("ZA", " ", 1000L)]
    [InlineData("ZA", "South Africa", 0L)]
    [InlineData("ZA", "South Africa", null)]
    public void ToCountries_InvalidRow_IsRejectedNamingTheRow(string? isoCode, string? name, long? population)
    {
        var file = FileWith(new CountrySeedEntryDto("FR", "France", 66_000_000), new CountrySeedEntryDto(isoCode, name, population));

        var exception = Assert.Throws<InvalidDataException>(() => CountrySeedMapper.ToCountries(file));

        Assert.Contains("row 2", exception.Message);
    }

    [Fact]
    public void ToCountries_DuplicateIsoCode_IsRejected()
    {
        var file = FileWith(new CountrySeedEntryDto("ZA", "South Africa", 1000), new CountrySeedEntryDto("ZA", "Zambia", 2000));

        Assert.Throws<InvalidDataException>(() => CountrySeedMapper.ToCountries(file));
    }

    private static CountrySeedFileDto FileWith(params CountrySeedEntryDto[] entries) =>
        new("https://example.org", "2026-10-30", entries);
}
