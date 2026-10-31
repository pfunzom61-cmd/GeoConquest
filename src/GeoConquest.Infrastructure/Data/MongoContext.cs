using GeoConquest.Core.Models;
using GeoConquest.Infrastructure.Configuration;
using GeoConquest.Infrastructure.Identity;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace GeoConquest.Infrastructure.Data;

/// <summary>
/// Single access point to the game database: names the collections and creates their indexes.
/// All repositories share the one <see cref="IMongoClient"/> registered for the application.
/// </summary>
public sealed class MongoContext
{
    public const string UsersCollectionName = "users";
    public const string RolesCollectionName = "roles";

    private const string GamesCollectionName = "games";
    private const string GameEventsCollectionName = "gameEvents";
    private const string CountriesCollectionName = "countries";
    private const string DataProtectionKeysCollectionName = "dataProtectionKeys";

    public MongoContext(IMongoClient client, IOptions<MongoDbOptions> options)
    {
        Database = client.GetDatabase(options.Value.DatabaseName);
    }

    public IMongoDatabase Database { get; }

    public IMongoCollection<Game> Games => Database.GetCollection<Game>(GamesCollectionName);

    /// <summary>The games collection as raw documents, used to replace a game and bump its version in one write.</summary>
    public IMongoCollection<BsonDocument> GameDocuments => Database.GetCollection<BsonDocument>(GamesCollectionName);

    public IMongoCollection<GameEvent> GameEvents => Database.GetCollection<GameEvent>(GameEventsCollectionName);

    public IMongoCollection<Country> Countries => Database.GetCollection<Country>(CountriesCollectionName);

    public IMongoCollection<ApplicationUser> Users => Database.GetCollection<ApplicationUser>(UsersCollectionName);

    public IMongoCollection<BsonDocument> DataProtectionKeys => Database.GetCollection<BsonDocument>(DataProtectionKeysCollectionName);

    /// <summary>
    /// Creates the indexes the application relies on. Safe to run on every start: existing indexes are left alone.
    /// Countries need none because the ISO code is their <c>_id</c>, which MongoDB keeps unique.
    /// </summary>
    public async Task EnsureIndexesAsync(CancellationToken cancellationToken)
    {
        var gameIndexes = Builders<Game>.IndexKeys;
        await Games.Indexes.CreateOneAsync(
            new CreateIndexModel<Game>(
                gameIndexes.Ascending(game => game.OwnerUserId).Ascending(game => game.Status),
                new CreateIndexOptions { Name = "ownerUserId_status" }),
            cancellationToken: cancellationToken);

        var eventIndexes = Builders<GameEvent>.IndexKeys;
        await GameEvents.Indexes.CreateOneAsync(
            new CreateIndexModel<GameEvent>(
                eventIndexes.Ascending(gameEvent => gameEvent.GameId).Ascending(gameEvent => gameEvent.Sequence),
                new CreateIndexOptions { Name = "gameId_sequence", Unique = true }),
            cancellationToken: cancellationToken);

        // Identity checks for a duplicate email before inserting; this closes the gap between that check and the insert.
        await Users.Indexes.CreateOneAsync(
            new CreateIndexModel<ApplicationUser>(
                Builders<ApplicationUser>.IndexKeys.Ascending(user => user.NormalizedEmail),
                new CreateIndexOptions { Name = "normalizedEmail", Unique = true }),
            cancellationToken: cancellationToken);
    }
}
