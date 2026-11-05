using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;

namespace GeoConquest.Web.Tests.Fakes;

/// <summary>A small fixed catalogue: enough countries (twelve) to start a ten-country game.</summary>
public sealed class InMemoryCountryRepository : ICountryRepository
{
    private readonly List<Country> _countries = Enumerable.Range(1, 12)
        .Select(number => new Country($"T{number:00}", $"Testland {number}", number * 10_000_000L))
        .ToList();

    public Task<IReadOnlyList<Country>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Country>>(_countries);

    public Task<Country?> GetByIsoCodeAsync(string isoCode, CancellationToken cancellationToken) =>
        Task.FromResult(_countries.FirstOrDefault(country => country.IsoCode == isoCode));

    public Task<long> CountAsync(CancellationToken cancellationToken) => Task.FromResult((long)_countries.Count);

    public Task AddManyAsync(IReadOnlyCollection<Country> countries, CancellationToken cancellationToken)
    {
        _countries.AddRange(countries);
        return Task.CompletedTask;
    }
}
