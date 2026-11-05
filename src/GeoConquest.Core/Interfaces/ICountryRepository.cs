using GeoConquest.Core.Models;

namespace GeoConquest.Core.Interfaces;

/// <summary>Stores the catalogue of countries a game can be played with.</summary>
public interface ICountryRepository
{
    /// <summary>Returns every country, ordered by name.</summary>
    Task<IReadOnlyList<Country>> GetAllAsync(CancellationToken cancellationToken);

    Task<Country?> GetByIsoCodeAsync(string isoCode, CancellationToken cancellationToken);

    Task<long> CountAsync(CancellationToken cancellationToken);

    Task AddManyAsync(IReadOnlyCollection<Country> countries, CancellationToken cancellationToken);
}
