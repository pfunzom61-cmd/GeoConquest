using GeoConquest.Core.Constants;
using GeoConquest.Core.Enums;
using GeoConquest.Core.Exceptions;
using GeoConquest.Core.Models;
using GeoConquest.Core.Services;
using GeoConquest.Core.Tests.Fakes;

namespace GeoConquest.Core.Tests.Services;

public sealed class BattleServiceTests
{
    /// <summary>Game where P0's C0 attacks P1's C1; script lists attacker dice then defender dice per round.</summary>
    private static (Game Game, BattleService Service) StartBattle(int attackerSoldiers, int defenderSoldiers, params int[][] script)
    {
        var game = GameBuilder.Create(attackerSoldiers, defenderSoldiers);
        game.BeginBattle("C0", "C1");
        return (game, new BattleService(new FixedDiceRoller(script)));
    }

    [Theory]
    [InlineData(10, 2, 2)]
    [InlineData(3, 5, 2)]
    [InlineData(2, 5, 1)]
    public void RollRound_SpecExamples_RollsMinimumOfAttackerLessOneAndDefender(int attackers, int defenders, int expectedDice)
    {
        var dice = Enumerable.Repeat(3, expectedDice).ToArray();
        var (game, service) = StartBattle(attackers, defenders, dice, dice);

        var round = service.RollRound(game);

        Assert.Equal(expectedDice, round.AttackerDice.Count);
        Assert.Equal(expectedDice, round.DefenderDice.Count);
    }

    [Fact]
    public void RollRound_SpecWorkedExample_AttackerLosesOneAndDefenderLosesOne()
    {
        var (game, service) = StartBattle(10, 2, [1, 4], [2, 3]);

        var round = service.RollRound(game);

        Assert.Equal(1, round.AttackerLosses);
        Assert.Equal(1, round.DefenderLosses);
        Assert.Equal(9, game.GetTerritory("C0").Soldiers);
        Assert.Equal(1, game.GetTerritory("C1").Soldiers);
        Assert.Equal(BattleOutcome.Continuing, round.Outcome);
    }

    [Fact]
    public void RollRound_DiceNotSorted_ComparesPairsInRollOrder()
    {
        // In roll order: 3v4 attacker loses, 5v2 defender loses. Sorted pairing would wrongly cost the defender both.
        var (game, service) = StartBattle(3, 5, [3, 5], [4, 2]);

        var round = service.RollRound(game);

        Assert.Equal(1, round.AttackerLosses);
        Assert.Equal(1, round.DefenderLosses);
    }

    [Fact]
    public void RollRound_AllTies_NobodyLosesSoldiers()
    {
        var (game, service) = StartBattle(3, 5, [4, 2], [4, 2]);

        var round = service.RollRound(game);

        Assert.Equal(0, round.AttackerLosses);
        Assert.Equal(0, round.DefenderLosses);
        Assert.Equal(3, game.GetTerritory("C0").Soldiers);
        Assert.Equal(5, game.GetTerritory("C1").Soldiers);
    }

    [Fact]
    public void RollRound_AttackerDropsToOneSoldier_AttackFailsAndNobodyChangesOwner()
    {
        var (game, service) = StartBattle(2, 5, [1], [6]);

        var round = service.RollRound(game);

        Assert.Equal(BattleOutcome.Failed, round.Outcome);
        Assert.Equal(1, game.GetTerritory("C0").Soldiers);
        Assert.Equal("P0", game.GetTerritory("C0").OwnerPlayerId);
        Assert.Equal(5, game.GetTerritory("C1").Soldiers);
        Assert.Equal("P1", game.GetTerritory("C1").OwnerPlayerId);
    }

    [Fact]
    public void RollRound_DefenderReachesZero_ConquersCountryWithRewardAsGarrison()
    {
        var (game, service) = StartBattle(5, 1, [6], [1]);

        var round = service.RollRound(game);

        var conquered = game.GetTerritory("C1");
        Assert.Equal(BattleOutcome.Conquered, round.Outcome);
        Assert.Equal("P0", conquered.OwnerPlayerId);
        Assert.Equal(2, conquered.Soldiers);
        Assert.Equal(5, game.GetTerritory("C0").Soldiers);
    }

    [Theory]
    [InlineData(9, 4)]
    [InlineData(1, 2)]
    [InlineData(4, 2)]
    [InlineData(6, 3)]
    [InlineData(5, 2)]
    public void RollRound_ConqueringCountry_RewardIsHalfItsSoldierValueWithMinimumTwo(int soldierValue, int expectedReward)
    {
        // The defender's value is fixed at its starting soldiers; reduce the garrison to one so a single win conquers it.
        var game = GameBuilder.Create(10, soldierValue);
        if (soldierValue > 1)
        {
            game.GetTerritory("C1").RemoveSoldiers(soldierValue - 1);
        }

        game.BeginBattle("C0", "C1");
        var service = new BattleService(new FixedDiceRoller([6], [1]));

        var round = service.RollRound(game);

        Assert.Equal(expectedReward, round.ConquestReward);
        Assert.Equal(expectedReward, game.GetTerritory("C1").Soldiers);
    }

    [Fact]
    public void RollRound_Continuing_RecordsRoundOnTheBattle()
    {
        var (game, service) = StartBattle(10, 5, [4, 4, 4, 4, 4], [3, 3, 3, 3, 3]);

        service.RollRound(game);

        Assert.Single(game.ActiveBattle!.Rounds);
    }

    [Fact]
    public void RollRound_NoActiveBattle_Throws()
    {
        var service = new BattleService(new FixedDiceRoller());

        Assert.Throws<GameRuleException>(() => service.RollRound(GameBuilder.Create()));
    }

    [Fact]
    public void RollRound_AttackerHasOnlyOneSoldier_Throws()
    {
        var (game, service) = StartBattle(1, 5);

        var exception = Assert.Throws<GameRuleException>(() => service.RollRound(game));

        Assert.Contains($"{GameRules.MinimumAttackingSoldiers} soldiers", exception.Message);
    }
}
