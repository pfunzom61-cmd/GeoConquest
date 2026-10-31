using System.Collections.Concurrent;
using GeoConquest.Core.Enums;
using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;

namespace GeoConquest.Web.Tests.Fakes;

/// <summary>Keeps games in memory so integration tests never reach MongoDB.</summary>
public sealed class InMemoryGameRepository : IGameRepository
{
    private readonly ConcurrentDictionary<string, Game> _games = new();

    public Task<Game?> GetByIdAsync(string gameId, CancellationToken cancellationToken) =>
        Task.FromResult(_games.GetValueOrDefault(gameId));

    public Task AddAsync(Game game, CancellationToken cancellationToken)
    {
        _games[game.Id] = game;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Game game, CancellationToken cancellationToken)
    {
        _games[game.Id] = game;
        game.AdvanceVersion();
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Game>> ListByOwnerAsync(string ownerUserId, GameStatus? status, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Game>>(_games.Values
            .Where(game => game.OwnerUserId == ownerUserId && (status is null || game.Status == status))
            .OrderByDescending(game => game.UpdatedAt)
            .ToList());

    public Task<IReadOnlyList<Game>> ListInProgressAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Game>>(_games.Values
            .Where(game => game.Status == GameStatus.InProgress)
            .OrderByDescending(game => game.UpdatedAt)
            .Take(IGameRepository.MaxInProgressGames)
            .ToList());
}
