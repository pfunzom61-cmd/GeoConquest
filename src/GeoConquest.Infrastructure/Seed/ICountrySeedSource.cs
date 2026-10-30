using GeoConquest.Core.Models;

namespace GeoConquest.Infrastructure.Seed;

/// <summary>Supplies the countries used to fill an empty catalogue.</summary>
public interface ICountrySeedSource
{
    Task<IReadOnlyList<Country>> LoadAsync(CancellationToken cancellationToken);
}
