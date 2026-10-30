using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;
using GeoConquest.Infrastructure.Data;
using MongoDB.Driver;

namespace GeoConquest.Infrastructure.Repositories;

/// <summary>MongoDB implementation of <see cref="ICountryRepository"/>.</summary>
public sealed class MongoCountryRepository(MongoContext context) : ICountryRepository
{
    public async Task<IReadOnlyList<Country>> GetAllAsync(CancellationToken cancellationToken) =>
        await context.Countries
            .Find(FilterDefinition<Country>.Empty)
            .SortBy(country => country.Name)
            .ToListAsync(cancellationToken);

    public async Task<Country?> GetByIsoCodeAsync(string isoCode, CancellationToken cancellationToken) =>
        await context.Countries
            .Find(country => country.IsoCode == isoCode)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<long> CountAsync(CancellationToken cancellationToken) =>
        context.Countries.CountDocumentsAsync(FilterDefinition<Country>.Empty, cancellationToken: cancellationToken);

    public async Task AddManyAsync(IReadOnlyCollection<Country> countries, CancellationToken cancellationToken)
    {
        if (countries.Count == 0)
        {
            return;
        }

        await context.Countries.InsertManyAsync(countries, cancellationToken: cancellationToken);
    }
}
