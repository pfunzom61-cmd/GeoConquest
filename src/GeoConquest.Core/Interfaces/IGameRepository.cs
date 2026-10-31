using GeoConquest.Core.Enums;
using GeoConquest.Core.Models;

namespace GeoConquest.Core.Interfaces;

/// <summary>Stores games. Each game is saved as one document, guarded by its <see cref="Game.Version"/>.</summary>
public interface IGameRepository
{
    /// <summary>The most games <see cref="ListInProgressAsync"/> returns, so a public page can never pull the whole collection.</summary>
    const int MaxInProgressGames = 50;

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

    /// <summary>Returns games still being played, most recently active first, at most <see cref="MaxInProgressGames"/> of them.</summary>
    Task<IReadOnlyList<Game>> ListInProgressAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns the ids of every game in progress whose current player is an NPC (see <see cref="Game.AwaitsNpcTurn"/>).
    /// Uncapped, unlike <see cref="ListInProgressAsync"/>: it is how play resumes after a restart.
    /// </summary>
    Task<IReadOnlyList<string>> ListGameIdsAwaitingNpcTurnAsync(CancellationToken cancellationToken);
}
