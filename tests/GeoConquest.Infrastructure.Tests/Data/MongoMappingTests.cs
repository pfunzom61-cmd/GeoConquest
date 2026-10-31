using GeoConquest.Core.Enums;
using GeoConquest.Core.Models;
using GeoConquest.Infrastructure.Data.Mapping;
using GeoConquest.Infrastructure.Identity;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace GeoConquest.Infrastructure.Tests.Data;

public sealed class MongoMappingTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    public MongoMappingTests() => MongoMappingConfiguration.Register();

    [Fact]
    public void Game_SavedAndLoaded_KeepsPlayersBoardBattleAndTurnState()
    {
        var game = CreateGameMidAttack();

        var loaded = BsonSerializer.Deserialize<Game>(game.ToBsonDocument());

        Assert.Equal(game.Id, loaded.Id);
        Assert.Equal(game.OwnerUserId, loaded.OwnerUserId);
        Assert.Equal(game.TurnOrder, loaded.TurnOrder);
        Assert.Equal(TurnPhase.Attacking, loaded.Phase);
        Assert.Equal(7, loaded.Version);
        Assert.Equal(3, loaded.LastEventSequence);
        Assert.Equal(CreatedAt, loaded.CreatedAt);
        Assert.True(loaded.GetPlayer("npc-FR").IsEliminated);
        Assert.Equal(5, loaded.GetTerritory("ZA").Soldiers);
        Assert.Equal("npc-FR", loaded.GetTerritory("FR").OwnerPlayerId);
        Assert.Equal("ZA", loaded.ActiveBattle!.AttackerCountryCode);

        var round = Assert.Single(loaded.ActiveBattle.Rounds);
        Assert.Equal([6, 2], round.AttackerDice);
        Assert.Equal(BattleOutcome.Continuing, round.Outcome);
    }

    [Fact]
    public void Game_Saved_StoresEnumsAsStringsAndDatesAsBsonDates()
    {
        var document = CreateGameMidAttack().ToBsonDocument();

        Assert.Equal("Attacking", document["phase"].AsString);
        Assert.Equal("InProgress", document["status"].AsString);
        Assert.Equal(BsonType.DateTime, document["createdAt"].BsonType);
        Assert.Equal("game-1", document["_id"].AsString);
    }

    [Fact]
    public void GameEvent_SavedAndLoaded_KeepsSequenceAndDicePayload()
    {
        var payload = new GameEventPayload { FromCountryCode = "ZA", ToCountryCode = "FR", AttackerDice = [6, 2], AttackerLosses = 1 };
        var gameEvent = new GameEvent("game-1", 4, GameEventType.DiceRolled, "human", "ZA rolled", payload, CreatedAt);

        var loaded = BsonSerializer.Deserialize<GameEvent>(gameEvent.ToBsonDocument());

        Assert.Equal(4, loaded.Sequence);
        Assert.Equal(GameEventType.DiceRolled, loaded.Type);
        Assert.Equal([6, 2], loaded.Payload!.AttackerDice);
        Assert.Null(loaded.Payload.DefenderDice);
        Assert.Equal(CreatedAt, loaded.CreatedAt);
    }

    // Null values are left out of stored documents, so each row is shaped like a real document with elements missing.
    [Theory]
    [MemberData(nameof(DocumentsWithOmittedNulls))]
    public void Document_SavedWithNullFieldsOmitted_LoadsBackUnchanged(object original)
    {
        var document = original.ToBsonDocument(original.GetType());

        var loaded = BsonSerializer.Deserialize(document, original.GetType());

        Assert.Equal(document, loaded.ToBsonDocument(original.GetType()));
    }

    public static TheoryData<object> DocumentsWithOmittedNulls() =>
    [
        new GameEvent("game-1", 1, GameEventType.GameCreated, null, "Bob takes command of South Africa.", null, CreatedAt),
        new GameEvent("game-1", 2, GameEventType.TurnStarted, "human", "Bob's turn.", null, CreatedAt),
        new GameEvent("game-1", 3, GameEventType.DiceRolled, "human", "ZA rolled", new GameEventPayload { AttackerDice = [6], DefenderLosses = 1 }, CreatedAt),
        CreateNewGame(),
    ];

    [Fact]
    public void ApplicationUser_SavedAndLoaded_KeepsGuidIdAndDisplayName()
    {
        var user = new ApplicationUser { UserName = "bob@example.org", DisplayName = "Bob" };

        var loaded = BsonSerializer.Deserialize<ApplicationUser>(user.ToBsonDocument());

        Assert.Equal(user.Id, loaded.Id);
        Assert.Equal("Bob", loaded.DisplayName);
    }

    private static Game CreateNewGame()
    {
        var players = new[]
        {
            new Player("human", "Bob", true, "ZA", "color-0"),
            new Player("npc-FR", "France", false, "FR", "color-1"),
        };
        var territories = new[]
        {
            new Territory("ZA", "South Africa", 2, "human", 5),
            new Territory("FR", "France", 3, "npc-FR", 4),
        };

        return new Game("game-1", "user-1", "Bob", players, territories, ["human", "npc-FR"], CreatedAt);
    }

    private static Game CreateGameMidAttack()
    {
        var game = CreateNewGame();

        game.GetPlayer("npc-FR").Eliminate();
        game.BeginBattle("ZA", "FR");
        game.ActiveBattle!.AddRound(new BattleRound([6, 2], [3, 3], 1, 1, BattleOutcome.Continuing, 0));
        game.NextEventSequence();
        game.NextEventSequence();
        game.NextEventSequence();
        for (var saves = 0; saves < 7; saves++)
        {
            game.AdvanceVersion();
        }

        return game;
    }
}
