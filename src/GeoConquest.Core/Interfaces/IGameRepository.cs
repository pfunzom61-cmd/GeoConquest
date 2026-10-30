using GeoConquest.Core.Enums;
using GeoConquest.Core.Models;

namespace GeoConquest.Core.Interfaces;

/// <summary>Stores games. Each game is saved as one document, guarded by its <see cref="Game.Version"/>.</summary>
public interface IGameRepository
{
    Task<Game?> GetByIdAsync(string gameId, CancellationToken cancellationToken);

    Task AddAsync(Game game, CancellationToken cancellationToken);

    /// <summary>
    /// Saves the game only if its stored version still equals <see cref="Game.Version"/>, writing the next
    /// version in the same atomic replace, then advances the game's own version.
    /// </summary>
    /// <exception cref="Exceptions.ConcurrencyException">The game was changed or removed since it was loaded.</exception>
    Task UpdateAsync(Game game, CancellationToken cancellationToken);

    /// <summary>Returns the owner's games, newest activity first, optionally only those with the given status.</summary>
    Task<IReadOnlyList<Game>> ListByOwnerAsync(string ownerUserId, GameStatus? status, CancellationToken cancellationToken);
}
