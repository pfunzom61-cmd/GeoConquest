using System.Collections.Concurrent;
using GeoConquest.Core.Enums;
using GeoConquest.Core.Exceptions;
using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;
using GeoConquest.Infrastructure.Data.Mapping;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace GeoConquest.Web.Tests.Fakes;

/// <summary>
/// Keeps games in memory so integration tests never reach MongoDB. Like the real repository it hands out copies
/// (through the same BSON mapping) and rejects a save whose version is stale, so a test cannot pass only because the
/// controller, the background runner and the test all share one object.
/// </summary>
public sealed class InMemoryGameRepository : IGameRepository
{
    private readonly ConcurrentDictionary<string, Game> _stored = new();
    private readonly object _saveLock = new();

    public InMemoryGameRepository() => MongoMappingConfiguration.Register();

    public Task<Game?> GetByIdAsync(string gameId, CancellationToken cancellationToken) =>
        Task.FromResult(_stored.TryGetValue(gameId, out var stored) ? Copy(stored) : null);

    public Task AddAsync(Game game, CancellationToken cancellationToken)
    {
        _stored[game.Id] = Copy(game);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Game game, CancellationToken cancellationToken)
    {
        lock (_saveLock)
        {
            if (!_stored.TryGetValue(game.Id, out var stored) || stored.Version != game.Version)
            {
                throw new ConcurrencyException("This game was changed somewhere else since you opened it. Reload it and try again.");
            }

            var next = Copy(game);
            next.AdvanceVersion();
            _stored[game.Id] = next;
            game.AdvanceVersion();
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Game>> ListByOwnerAsync(string ownerUserId, GameStatus? status, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Game>>(_stored.Values
            .Where(game => game.OwnerUserId == ownerUserId && (status is null || game.Status == status))
            .OrderByDescending(game => game.UpdatedAt)
            .Select(Copy)
            .ToList());

    public Task<IReadOnlyList<Game>> ListPublicAsync(LiveGameStatusFilter statusFilter, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Game>>(_stored.Values
            .Where(game => statusFilter switch
            {
                LiveGameStatusFilter.All => true,
                LiveGameStatusFilter.Finished => game.Status != GameStatus.InProgress,
                _ => game.Status == GameStatus.InProgress,
            })
            .OrderByDescending(game => game.UpdatedAt)
            .Take(IGameRepository.MaxPublicGames)
            .Select(Copy)
            .ToList());

    public Task<IReadOnlyList<string>> ListGameIdsAwaitingNpcTurnAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<string>>(_stored.Values.Where(game => game.AwaitsNpcTurn).Select(game => game.Id).ToList());

    private static Game Copy(Game game) => BsonSerializer.Deserialize<Game>(game.ToBsonDocument());
}
