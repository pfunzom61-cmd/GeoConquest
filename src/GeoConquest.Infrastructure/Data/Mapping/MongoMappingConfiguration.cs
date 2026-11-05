using GeoConquest.Core.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace GeoConquest.Infrastructure.Data.Mapping;

/// <summary>
/// Teaches the MongoDB driver how to store the Core models, so Core needs no Mongo attributes.
/// Models expose private setters and constructor-only state, so every class is mapped explicitly:
/// the driver rebuilds each object through its public constructor and then restores the remaining state.
/// </summary>
public static class MongoMappingConfiguration
{
    private static readonly object SyncLock = new();
    private static bool _registered;

    /// <summary>Registers conventions and class maps once per process; later calls do nothing.</summary>
    public static void Register()
    {
        lock (SyncLock)
        {
            if (_registered)
            {
                return;
            }

            // Identity user ids are Guids, and the driver refuses to store a Guid until a format is chosen.
            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

            MapCountry();
            MapPlayer();
            MapTerritory();
            MapBattleRound();
            MapBattleState();
            MapGame();
            MapGameEventPayload();
            MapGameEvent();

            _registered = true;
        }
    }

    // Explicitly mapped members skip the driver's conventions, so the storage format is applied here:
    // camelCase element names, enums as strings, and null values left out of the document.
    private static void RegisterMap<T>(Action<BsonClassMap<T>> configure) =>
        BsonClassMap.RegisterClassMap<T>(map =>
        {
            configure(map);

            foreach (var member in map.DeclaredMemberMaps)
            {
                if (member.ElementName != "_id")
                {
                    member.SetElementName(char.ToLowerInvariant(member.MemberName[0]) + member.MemberName[1..]);
                }

                var enumType = Nullable.GetUnderlyingType(member.MemberType) ?? member.MemberType;
                if (enumType.IsEnum)
                {
                    var serializerType = typeof(EnumSerializer<>).MakeGenericType(enumType);
                    var serializer = (IBsonSerializer)Activator.CreateInstance(serializerType, BsonType.String)!;
                    member.SetSerializer(Nullable.GetUnderlyingType(member.MemberType) is null
                        ? serializer
                        : (IBsonSerializer)Activator.CreateInstance(typeof(NullableSerializer<>).MakeGenericType(enumType), serializer)!);
                }

                member.SetIgnoreIfNull(true);
            }
        });

    // Stored as BSON dates (not the driver's default [ticks, offset] array) so they can be sorted and queried.
    private static DateTimeOffsetSerializer DateSerializer => new(BsonType.DateTime);

    private static void MapCountry() =>
        RegisterMap<Country>(map =>
        {
            map.MapIdMember(country => country.IsoCode);
            map.MapMember(country => country.Name);
            map.MapMember(country => country.Population);
            map.MapConstructor(
                typeof(Country).GetConstructor([typeof(string), typeof(string), typeof(long)])!,
                nameof(Country.IsoCode), nameof(Country.Name), nameof(Country.Population));
        });

    private static void MapPlayer() =>
        RegisterMap<Player>(map =>
        {
            map.MapMember(player => player.Id);
            map.MapMember(player => player.DisplayName);
            map.MapMember(player => player.IsHuman);
            map.MapMember(player => player.HomeCountryCode);
            map.MapMember(player => player.ColorKey);
            map.MapMember(player => player.IsEliminated);
            map.MapConstructor(
                typeof(Player).GetConstructor([typeof(string), typeof(string), typeof(bool), typeof(string), typeof(string)])!,
                nameof(Player.Id), nameof(Player.DisplayName), nameof(Player.IsHuman), nameof(Player.HomeCountryCode), nameof(Player.ColorKey));
        });

    private static void MapTerritory() =>
        RegisterMap<Territory>(map =>
        {
            map.MapMember(territory => territory.CountryCode);
            map.MapMember(territory => territory.CountryName);
            map.MapMember(territory => territory.SoldierValue);
            map.MapMember(territory => territory.OwnerPlayerId);
            map.MapMember(territory => territory.Soldiers);
            map.MapConstructor(
                typeof(Territory).GetConstructor([typeof(string), typeof(string), typeof(int), typeof(string), typeof(int)])!,
                nameof(Territory.CountryCode), nameof(Territory.CountryName), nameof(Territory.SoldierValue),
                nameof(Territory.OwnerPlayerId), nameof(Territory.Soldiers));
        });

    private static void MapBattleRound() =>
        RegisterMap<BattleRound>(map =>
        {
            map.MapMember(round => round.AttackerDice);
            map.MapMember(round => round.DefenderDice);
            map.MapMember(round => round.AttackerLosses);
            map.MapMember(round => round.DefenderLosses);
            map.MapMember(round => round.Outcome);
            map.MapMember(round => round.ConquestReward);
            map.MapConstructor(
                typeof(BattleRound).GetConstructors().Single(),
                nameof(BattleRound.AttackerDice), nameof(BattleRound.DefenderDice), nameof(BattleRound.AttackerLosses),
                nameof(BattleRound.DefenderLosses), nameof(BattleRound.Outcome), nameof(BattleRound.ConquestReward));
        });

    private static void MapBattleState() =>
        RegisterMap<BattleState>(map =>
        {
            map.MapMember(battle => battle.AttackerCountryCode);
            map.MapMember(battle => battle.DefenderCountryCode);
            map.MapMember(battle => battle.Rounds);
            map.MapCreator(battle => RestoreBattle(battle.AttackerCountryCode, battle.DefenderCountryCode, battle.Rounds));
        });

    // Rounds can only be added one by one, so loading replays them onto a fresh battle.
    private static BattleState RestoreBattle(string attackerCountryCode, string defenderCountryCode, IReadOnlyList<BattleRound> rounds)
    {
        var battle = new BattleState(attackerCountryCode, defenderCountryCode);
        foreach (var round in rounds)
        {
            battle.AddRound(round);
        }

        return battle;
    }

    private static void MapGame() =>
        RegisterMap<Game>(map =>
        {
            map.MapIdMember(game => game.Id);
            map.MapMember(game => game.OwnerUserId);
            map.MapMember(game => game.OwnerDisplayName);
            map.MapMember(game => game.Players);
            map.MapMember(game => game.Territories);
            map.MapMember(game => game.TurnOrder);
            map.MapMember(game => game.CreatedAt).SetSerializer(DateSerializer);
            map.MapMember(game => game.UpdatedAt).SetSerializer(DateSerializer);
            map.MapMember(game => game.Status);
            map.MapMember(game => game.CurrentTurnIndex);
            map.MapMember(game => game.RoundNumber);
            map.MapMember(game => game.Phase);
            map.MapMember(game => game.PendingReinforcements);
            map.MapMember(game => game.ActiveBattle);
            map.MapMember(game => game.IsFastForward);
            map.MapMember(game => game.WinnerPlayerId);
            map.MapMember(game => game.LastEventSequence);
            map.MapMember(game => game.Version);
            map.MapConstructor(
                typeof(Game).GetConstructors().Single(),
                nameof(Game.Id), nameof(Game.OwnerUserId), nameof(Game.OwnerDisplayName), nameof(Game.Players),
                nameof(Game.Territories), nameof(Game.TurnOrder), nameof(Game.CreatedAt));
        });

    private static void MapGameEventPayload() =>
        RegisterMap<GameEventPayload>(map =>
        {
            map.MapMember(payload => payload.FromCountryCode);
            map.MapMember(payload => payload.ToCountryCode);
            map.MapMember(payload => payload.Soldiers);
            map.MapMember(payload => payload.AttackerDice);
            map.MapMember(payload => payload.DefenderDice);
            map.MapMember(payload => payload.AttackerLosses);
            map.MapMember(payload => payload.DefenderLosses);
            map.MapMember(payload => payload.AttackerSoldiersBefore);
            map.MapMember(payload => payload.AttackerSoldiersAfter);
            map.MapMember(payload => payload.DefenderSoldiersBefore);
            map.MapMember(payload => payload.DefenderSoldiersAfter);
            map.MapMember(payload => payload.BattleRoundNumber);
            map.MapMember(payload => payload.GameRoundNumber);
            map.MapMember(payload => payload.BattleAttackerLosses);
            map.MapMember(payload => payload.BattleDefenderLosses);
            map.MapMember(payload => payload.Outcome);
            map.MapMember(payload => payload.CommentarySource);
        });

    private static void MapGameEvent() =>
        RegisterMap<GameEvent>(map =>
        {
            map.MapMember(gameEvent => gameEvent.GameId);
            map.MapMember(gameEvent => gameEvent.Sequence);
            map.MapMember(gameEvent => gameEvent.Type);
            // Null values are not stored, so these two constructor arguments must be allowed to be absent on load.
            map.MapMember(gameEvent => gameEvent.ActorPlayerId).SetDefaultValue((object?)null);
            map.MapMember(gameEvent => gameEvent.Message);
            map.MapMember(gameEvent => gameEvent.Payload).SetDefaultValue((object?)null);
            map.MapMember(gameEvent => gameEvent.CreatedAt).SetSerializer(DateSerializer);
            map.MapConstructor(
                typeof(GameEvent).GetConstructors().Single(),
                nameof(GameEvent.GameId), nameof(GameEvent.Sequence), nameof(GameEvent.Type), nameof(GameEvent.ActorPlayerId),
                nameof(GameEvent.Message), nameof(GameEvent.Payload), nameof(GameEvent.CreatedAt));
            map.SetIgnoreExtraElements(true); // the server-generated _id is not part of the model
        });
}
