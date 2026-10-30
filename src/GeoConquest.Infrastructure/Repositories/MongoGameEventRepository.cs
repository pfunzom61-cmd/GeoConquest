using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;
using GeoConquest.Infrastructure.Data;
using MongoDB.Driver;

namespace GeoConquest.Infrastructure.Repositories;

/// <summary>MongoDB implementation of <see cref="IGameEventRepository"/>. Events are only ever appended.</summary>
public sealed class MongoGameEventRepository(MongoContext context) : IGameEventRepository
{
    private const int MaxPageSize = 100;

    public async Task AppendAsync(IReadOnlyCollection<GameEvent> events, CancellationToken cancellationToken)
    {
        if (events.Count == 0)
        {
            return;
        }

        await context.GameEvents.InsertManyAsync(events, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<GameEvent>> GetAfterSequenceAsync(string gameId, int afterSequence, CancellationToken cancellationToken) =>
        await context.GameEvents
            .Find(gameEvent => gameEvent.GameId == gameId && gameEvent.Sequence > afterSequence)
            .SortBy(gameEvent => gameEvent.Sequence)
            .ToListAsync(cancellationToken);

    public async Task<PagedResult<GameEvent>> GetPageAsync(string gameId, int page, int pageSize, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, MaxPageSize);

        var forThisGame = Builders<GameEvent>.Filter.Eq(gameEvent => gameEvent.GameId, gameId);
        var total = await context.GameEvents.CountDocumentsAsync(forThisGame, cancellationToken: cancellationToken);
        var items = await context.GameEvents
            .Find(forThisGame)
            .SortByDescending(gameEvent => gameEvent.Sequence)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<GameEvent>(items, (int)total, page, pageSize);
    }
}
