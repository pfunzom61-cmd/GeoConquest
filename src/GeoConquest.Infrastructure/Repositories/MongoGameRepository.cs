using GeoConquest.Core.Enums;
using GeoConquest.Core.Exceptions;
using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;
using GeoConquest.Infrastructure.Data;
using MongoDB.Bson;
using MongoDB.Driver;

namespace GeoConquest.Infrastructure.Repositories;

/// <summary>MongoDB implementation of <see cref="IGameRepository"/>. A game is one document.</summary>
public sealed class MongoGameRepository(MongoContext context) : IGameRepository
{
    public async Task<Game?> GetByIdAsync(string gameId, CancellationToken cancellationToken) =>
        await context.Games.Find(game => game.Id == gameId).FirstOrDefaultAsync(cancellationToken);

    public Task AddAsync(Game game, CancellationToken cancellationToken) =>
        context.Games.InsertOneAsync(game, cancellationToken: cancellationToken);

    public async Task UpdateAsync(Game game, CancellationToken cancellationToken)
    {
        // The next version is written inside the same replace that checks the current one, so the
        // check-and-increment is a single atomic operation on the server.
        var document = game.ToBsonDocument();
        document["version"] = game.Version + 1;

        var filter = Builders<BsonDocument>.Filter.Eq("_id", game.Id)
            & Builders<BsonDocument>.Filter.Eq("version", game.Version);
        var result = await context.GameDocuments.ReplaceOneAsync(filter, document, cancellationToken: cancellationToken);

        if (result.MatchedCount == 0)
        {
            throw new ConcurrencyException("This game was changed somewhere else since you opened it. Reload it and try again.");
        }

        game.AdvanceVersion();
    }

    public async Task<IReadOnlyList<Game>> ListByOwnerAsync(string ownerUserId, GameStatus? status, CancellationToken cancellationToken)
    {
        var filter = Builders<Game>.Filter.Eq(game => game.OwnerUserId, ownerUserId);
        if (status is { } requiredStatus)
        {
            filter &= Builders<Game>.Filter.Eq(game => game.Status, requiredStatus);
        }

        return await context.Games
            .Find(filter)
            .SortByDescending(game => game.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Game>> ListInProgressAsync(CancellationToken cancellationToken) =>
        await context.Games
            .Find(game => game.Status == GameStatus.InProgress)
            .SortByDescending(game => game.UpdatedAt)
            .Limit(IGameRepository.MaxInProgressGames)
            .ToListAsync(cancellationToken);
}
