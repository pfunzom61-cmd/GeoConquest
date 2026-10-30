using GeoConquest.Core.Enums;
using GeoConquest.Core.Exceptions;
using GeoConquest.Core.Models;
using GeoConquest.Core.Services;
using GeoConquest.Core.Tests.Fakes;

namespace GeoConquest.Core.Tests.Services;

public sealed class GameSetupServiceTests
{
    private readonly GameSetupService _service = new(
        new SoldierAllocator(), new ReversingShuffler(), new FixedTimeProvider(GameBuilder.Now));

    /// <summary>Countries X00..X11 with populations 10M, 20M ... 120M.</summary>
    private static List<Country> CreateCatalogue(int count = 12) =>
        Enumerable.Range(0, count)
            .Select(index => new Country($"X{index:00}", $"Land {index:00}", (index + 1) * 10_000_000L))
            .ToList();

    [Fact]
    public void CreateGame_ValidCountry_HasTenDistinctCountriesIncludingTheHumans()
    {
        var game = _service.CreateGame("user-1", "Bob", "X03", CreateCatalogue());

        Assert.Equal(10, game.Territories.Count);
        Assert.Equal(10, game.Territories.Select(territory => territory.CountryCode).Distinct().Count());
        Assert.Contains(game.Territories, territory => territory.CountryCode == "X03");
    }

    [Fact]
    public void CreateGame_ValidCountry_OneHumanOwnsChosenCountryAndNineNpcsOwnTheRest()
    {
        var game = _service.CreateGame("user-1", "Bob", "X03", CreateCatalogue());

        var human = Assert.Single(game.Players, player => player.IsHuman);
        Assert.Equal(9, game.Players.Count(player => !player.IsHuman));
        Assert.Equal("Bob", human.DisplayName);
        Assert.Equal("X03", human.HomeCountryCode);
        Assert.Equal(human.Id, game.GetTerritory("X03").OwnerPlayerId);
        Assert.All(game.Players, player =>
            Assert.Single(game.TerritoriesOwnedBy(player.Id), territory => territory.CountryCode == player.HomeCountryCode));
    }

    [Fact]
    public void CreateGame_ValidCountry_StartsEachCountryWithItsPopulationTierSoldiers()
    {
        var game = _service.CreateGame("user-1", "Bob", "X00", CreateCatalogue());

        // X00 has 10M people (tier 2); every catalogue country falls in tiers 2 to 7 (10M to 120M).
        Assert.Equal(2, game.GetTerritory("X00").Soldiers);
        Assert.All(game.Territories, territory => Assert.Equal(territory.SoldierValue, territory.Soldiers));
        Assert.All(game.Territories, territory => Assert.InRange(territory.Soldiers, 2, 7));
    }

    [Fact]
    public void CreateGame_ValidCountry_UsesShufflerForOpponentsAndTurnOrder()
    {
        var game = _service.CreateGame("user-1", "Bob", "X00", CreateCatalogue());

        // Reversed catalogue minus X00 starts X11, X10 ... so the nine NPCs are X11..X03.
        Assert.DoesNotContain(game.Territories, territory => territory.CountryCode is "X01" or "X02");
        Assert.Equal("human", game.TurnOrder[^1]);
        Assert.Equal("npc-X03", game.TurnOrder[0]);
    }

    [Fact]
    public void CreateGame_ValidCountry_StartsInRoundOneWithGameInProgress()
    {
        var game = _service.CreateGame("user-1", "Bob", "X00", CreateCatalogue());

        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Equal(1, game.RoundNumber);
        Assert.Equal(TurnPhase.Action, game.Phase);
        Assert.Equal("user-1", game.OwnerUserId);
        Assert.Equal(GameBuilder.Now, game.CreatedAt);
    }

    [Fact]
    public void CreateGame_CountryNotInCatalogue_Throws()
    {
        Assert.Throws<GameRuleException>(() => _service.CreateGame("user-1", "Bob", "ZZZ", CreateCatalogue()));
    }

    [Fact]
    public void CreateGame_CatalogueSmallerThanTen_Throws()
    {
        Assert.Throws<GameRuleException>(() => _service.CreateGame("user-1", "Bob", "X00", CreateCatalogue(count: 9)));
    }
}
