using GeoConquest.Core.Enums;
using GeoConquest.Core.Exceptions;
using GeoConquest.Core.Models;
using GeoConquest.Core.Services;
using GeoConquest.Core.Tests.Fakes;

namespace GeoConquest.Core.Tests.Services;

public sealed class ReinforcementServiceTests
{
    private readonly ReinforcementService _service = new();

    /// <summary>P0 owns C0 (5 soldiers) and C1 (5 soldiers) and has 4 reinforcements to place.</summary>
    private static Game CreateGameAwaitingReinforcements()
    {
        var game = GameBuilder.Create(5, 5, 5);
        game.GetTerritory("C1").ChangeOwner("P0", 5);
        game.StartTurn(reinforcements: 4);
        return game;
    }

    [Theory]
    [InlineData(1, 1, 0)]
    [InlineData(1, 2, 2)]
    [InlineData(3, 2, 6)]
    [InlineData(5, 7, 10)]
    [InlineData(3, 1, 0)]
    public void CalculateReinforcements_ByCountriesAndRound_FollowsTwoPerCountryFromRoundTwo(
        int ownedCountries, int round, int expected)
    {
        Assert.Equal(expected, _service.CalculateReinforcements(ownedCountries, round));
    }

    [Fact]
    public void Allocate_ExactSplitAcrossOwnedCountries_AddsSoldiersAndMovesToActionPhase()
    {
        var game = CreateGameAwaitingReinforcements();

        _service.Allocate(game, "P0", new Dictionary<string, int> { ["C0"] = 3, ["C1"] = 1 });

        Assert.Equal(8, game.GetTerritory("C0").Soldiers);
        Assert.Equal(6, game.GetTerritory("C1").Soldiers);
        Assert.Equal(0, game.PendingReinforcements);
        Assert.Equal(TurnPhase.Action, game.Phase);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    public void Allocate_TotalDiffersFromOwed_ThrowsAndChangesNothing(int total)
    {
        var game = CreateGameAwaitingReinforcements();

        Assert.Throws<GameRuleException>(() =>
            _service.Allocate(game, "P0", new Dictionary<string, int> { ["C0"] = total }));

        Assert.Equal(5, game.GetTerritory("C0").Soldiers);
        Assert.Equal(TurnPhase.Reinforce, game.Phase);
    }

    [Fact]
    public void Allocate_EnemyCountry_Throws()
    {
        var game = CreateGameAwaitingReinforcements();

        Assert.Throws<GameRuleException>(() =>
            _service.Allocate(game, "P0", new Dictionary<string, int> { ["C0"] = 2, ["C2"] = 2 }));

        Assert.Equal(5, game.GetTerritory("C2").Soldiers);
    }

    [Fact]
    public void Allocate_UnknownCountry_Throws()
    {
        var game = CreateGameAwaitingReinforcements();

        Assert.Throws<GameRuleException>(() =>
            _service.Allocate(game, "P0", new Dictionary<string, int> { ["XX"] = 4 }));
    }

    [Fact]
    public void Allocate_NegativeAmountBalancedByLargerOne_Throws()
    {
        var game = CreateGameAwaitingReinforcements();

        Assert.Throws<GameRuleException>(() =>
            _service.Allocate(game, "P0", new Dictionary<string, int> { ["C0"] = 7, ["C1"] = -3 }));

        Assert.Equal(5, game.GetTerritory("C0").Soldiers);
    }

    [Fact]
    public void Allocate_ZeroAmountEntry_Throws()
    {
        var game = CreateGameAwaitingReinforcements();

        Assert.Throws<GameRuleException>(() =>
            _service.Allocate(game, "P0", new Dictionary<string, int> { ["C0"] = 4, ["C1"] = 0 }));
    }

    [Fact]
    public void Allocate_NoReinforcementsOwed_Throws()
    {
        var game = GameBuilder.Create();

        Assert.Throws<GameRuleException>(() =>
            _service.Allocate(game, "P0", new Dictionary<string, int> { ["C0"] = 2 }));
    }
}
