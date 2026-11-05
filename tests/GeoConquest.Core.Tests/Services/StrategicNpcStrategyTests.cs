using GeoConquest.Core.Models;
using GeoConquest.Core.Services;
using GeoConquest.Core.Tests.Fakes;

namespace GeoConquest.Core.Tests.Services;

/// <summary>
/// Boards are built with <see cref="GameBuilder"/>: P0 is the human, P1 is the NPC under test, and each country
/// starts with soldiers equal to its soldier value. Win chances quoted in comments come from the exact odds calculator.
/// </summary>
public sealed class StrategicNpcStrategyTests
{
    private readonly StrategicNpcStrategy _strategy = new(new BattleOddsCalculator());

    private static void Own(Game game, string countryCode, string playerId) =>
        game.GetTerritory(countryCode).ChangeOwner(playerId, game.GetTerritory(countryCode).Soldiers);

    private static void AdvanceToRound(Game game, int round)
    {
        while (game.RoundNumber < round)
        {
            game.AdvanceToNextLivingPlayer();
        }
    }

    // ---- Reinforcements ----

    [Fact]
    public void DecideReinforcements_NoCountryAtRisk_PlacesEverythingOnTheStrongestCountry()
    {
        var game = GameBuilder.Create(3, 8, 2, 3);
        Own(game, "C3", "P1");
        game.StartTurn(6);

        var decision = _strategy.DecideReinforcements(game, game.GetPlayer("P1"));

        Assert.Equal(new Dictionary<string, int> { ["C1"] = 6 }, decision.Allocations);
    }

    [Fact]
    public void DecideReinforcements_CountryAtRisk_TopsItUpThenBacksTheStrongestCountry()
    {
        // The enemy stack of 9 takes a 6-soldier country 71% of the time but a 7-soldier one only 60%.
        var game = GameBuilder.Create(9, 6, 2, 2, 7);
        Own(game, "C4", "P1");
        game.StartTurn(4);

        var decision = _strategy.DecideReinforcements(game, game.GetPlayer("P1"));

        Assert.Equal(new Dictionary<string, int> { ["C1"] = 1, ["C4"] = 3 }, decision.Allocations);
    }

    [Fact]
    public void DecideReinforcements_CountryCannotBeSaved_DoesNotWasteSoldiersOnIt()
    {
        var game = GameBuilder.Create(9, 2, 2, 2, 8);
        Own(game, "C4", "P1");
        game.StartTurn(4);

        var decision = _strategy.DecideReinforcements(game, game.GetPlayer("P1"));

        Assert.Equal(new Dictionary<string, int> { ["C4"] = 4 }, decision.Allocations);
    }

    [Fact]
    public void DecideReinforcements_ThreatenedCountryIsAlsoTheStrongestBase_JustifiesItOnce()
    {
        // Country 1 needs 1 soldier to be safe from the stack of 9 and is also the strongest base, so it gets 1 + 3.
        var game = GameBuilder.Create(9, 6, 2, 2, 2);
        Own(game, "C4", "P1");
        game.StartTurn(4);

        var decision = _strategy.DecideReinforcements(game, game.GetPlayer("P1"));

        Assert.Equal(4, decision.Allocations["C1"]);
        Assert.Contains("4 in Country 1", decision.Justification);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(decision.Justification, "in Country 1"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(9)]
    public void DecideReinforcements_AnyAmount_PlacesExactlyAllPendingSoldiersOnOwnedCountries(int pending)
    {
        var game = GameBuilder.Create(9, 6, 2, 2, 7);
        Own(game, "C4", "P1");
        game.StartTurn(pending);

        var decision = _strategy.DecideReinforcements(game, game.GetPlayer("P1"));

        Assert.Equal(pending, decision.Allocations.Values.Sum());
        Assert.All(decision.Allocations.Keys, countryCode => Assert.Equal("P1", game.GetTerritory(countryCode).OwnerPlayerId));
    }

    // ---- Choosing an attack ----

    [Fact]
    public void DecideAttack_TargetsWithEqualRewards_PicksTheOneWithTheBestOdds()
    {
        var game = GameBuilder.Create(3, 8, 3, 2);

        var decision = _strategy.DecideAttack(game, game.GetPlayer("P1"));

        Assert.Equal("C3", decision.DefenderCountryCode); // 96% against 2 soldiers beats 91% against 3
    }

    [Fact]
    public void DecideAttack_RewardOutweighsSlightlyWorseOdds_PicksTheRicherTarget()
    {
        var game = GameBuilder.Create(3, 10, 2, 8);
        game.GetTerritory("C3").RemoveSoldiers(3); // value 8 (reward 4) but only 5 soldiers: 87% odds

        var decision = _strategy.DecideAttack(game, game.GetPlayer("P1"));

        Assert.Equal("C3", decision.DefenderCountryCode); // 0.87 x 5 beats the 99% target worth 0.99 x 3
    }

    [Theory]
    [InlineData(8, 6, true)] // 61% win chance
    [InlineData(10, 8, false)] // 60% rounds to the threshold but is actually 59.8%
    public void DecideAttack_OddsAroundTheThreshold_AttacksOnlyFromSixtyPercent(int attackerSoldiers, int defenderSoldiers, bool expectedAttack)
    {
        var game = GameBuilder.Create(3, attackerSoldiers, defenderSoldiers);

        var decision = _strategy.DecideAttack(game, game.GetPlayer("P1"));

        Assert.Equal(expectedAttack, decision.ShouldAttack);
    }

    [Fact]
    public void DecideAttack_OwnersLastCountry_BeatsAnEqualTargetThatLeavesTheOwnerAlive()
    {
        var game = GameBuilder.Create(3, 10, 4, 4, 4);
        Own(game, "C4", "P2"); // P2 keeps a second country, P3 would lose its last

        var decision = _strategy.DecideAttack(game, game.GetPlayer("P1"));

        Assert.Equal("C3", decision.DefenderCountryCode);
        Assert.Contains("eliminating", decision.Justification);
    }

    [Fact]
    public void DecideAttack_OwnerLeadsTheGame_BeatsAnEqualTargetOfAnotherOwner()
    {
        var game = GameBuilder.Create(3, 10, 4, 4, 4, 4);
        Own(game, "C4", "P2");
        Own(game, "C5", "P3");
        game.GetTerritory("C5").AddSoldiers(1); // P3 now holds 9 soldiers to P2's 8, so P3 leads

        var decision = _strategy.DecideAttack(game, game.GetPlayer("P1"));

        Assert.Equal("C3", decision.DefenderCountryCode);
        Assert.Contains("leader", decision.Justification);
    }

    [Theory]
    [InlineData(1, "C2")] // the human's 2 soldiers are the easiest target but protected in round 1
    [InlineData(2, "C0")]
    public void DecideAttack_HumanTarget_ProtectedInRoundOneOnly(int round, string expectedTarget)
    {
        var game = GameBuilder.Create(2, 10, 4);
        AdvanceToRound(game, round);

        var decision = _strategy.DecideAttack(game, game.GetPlayer("P1"));

        Assert.Equal(expectedTarget, decision.DefenderCountryCode);
    }

    [Fact]
    public void DecideAttack_NoCountryHasTwoSoldiers_HoldsAndSaysWhy()
    {
        var game = GameBuilder.Create(3, 1, 3);

        var decision = _strategy.DecideAttack(game, game.GetPlayer("P1"));

        Assert.False(decision.ShouldAttack);
        Assert.Contains("0 countries with 2+ soldiers", decision.Justification);
    }

    [Fact]
    public void DecideAttack_OwnCountryLooksEasiest_StillAttacksAnEnemy()
    {
        var game = GameBuilder.Create(3, 10, 2, 2);
        Own(game, "C2", "P1");

        var decision = _strategy.DecideAttack(game, game.GetPlayer("P1"));

        Assert.Equal("C3", decision.DefenderCountryCode);
    }

    [Fact]
    public void DecideAttack_StacksBeyondTheOddsCap_KeepTheirRatio()
    {
        // Capping each side separately at 60 would turn 200 v 100 into an even 60 v 60 fight (47%) and hold.
        var game = GameBuilder.Create(3, 200, 100);

        var decision = _strategy.DecideAttack(game, game.GetPlayer("P1"));

        Assert.True(decision.ShouldAttack);
    }

    // ---- Continuing an attack ----

    [Theory]
    [InlineData(5, 5, true)] // 50%
    [InlineData(4, 5, false)] // 23%, below the 35% floor
    public void DecideContinueAttack_OddsAroundTheFloor_StopsBelowThirtyFivePercent(int attackerSoldiers, int defenderSoldiers, bool expectedKeepRolling)
    {
        var game = GameBuilder.Create(3, attackerSoldiers, defenderSoldiers);
        game.BeginBattle("C1", "C2");

        var decision = _strategy.DecideContinueAttack(game, game.GetPlayer("P1"));

        Assert.Equal(expectedKeepRolling, decision.KeepRolling);
    }

    // ---- Transfers ----

    [Fact]
    public void DecideTransferBeforeAttack_SurplusUnlocksAnAttack_MovesIt()
    {
        // 7 v 8 is 40% and 5 v 8 is 11%, but pooling 5 + 7 into one country gives 11 v 8 at 69%.
        var game = GameBuilder.Create(3, 5, 8, 7);
        Own(game, "C3", "P1");

        var decision = _strategy.DecideTransferBeforeAttack(game, game.GetPlayer("P1"));

        Assert.NotNull(decision);
        Assert.Equal(("C1", "C3", 4), (decision.FromCountryCode, decision.ToCountryCode, decision.Soldiers));
    }

    [Theory]
    [InlineData(10, 3, 2)] // an attack at 99% is already possible
    [InlineData(3, 3, 12)] // pooled 5 soldiers would still be hopeless against 12
    public void DecideTransferBeforeAttack_AttackAlreadyPossibleOrStillOutOfReach_ReturnsNull(int firstCountry, int secondCountry, int enemy)
    {
        var game = GameBuilder.Create(3, firstCountry, enemy, secondCountry);
        Own(game, "C3", "P1");

        var decision = _strategy.DecideTransferBeforeAttack(game, game.GetPlayer("P1"));

        Assert.Null(decision);
    }

    [Theory]
    [InlineData(10, 2, 4)] // levels 10 v 2 to 6 v 6
    [InlineData(3, 2, 0)] // already level enough: moving would leave the attacker too thin
    public void DecideTransferAfterConquest_UnevenCountries_MovesHalfTheDifference(int attackerSoldiers, int conqueredSoldiers, int expectedMoved)
    {
        var game = GameBuilder.Create(3, attackerSoldiers, conqueredSoldiers);
        Own(game, "C2", "P1");

        var decision = _strategy.DecideTransferAfterConquest(game, game.GetPlayer("P1"), "C1", "C2");

        Assert.Equal(expectedMoved, decision?.Soldiers ?? 0);
    }

    // ---- Justifications ----

    [Fact]
    public void EveryDecision_Justification_QuotesItsNumbers()
    {
        var game = GameBuilder.Create(3, 10, 2, 4);
        var npc = game.GetPlayer("P1");
        game.StartTurn(4);
        var reinforcement = _strategy.DecideReinforcements(game, npc).Justification;
        var attack = _strategy.DecideAttack(game, npc);
        game.CompleteReinforcement();
        game.BeginBattle(attack.AttackerCountryCode!, attack.DefenderCountryCode!);
        var keepRolling = _strategy.DecideContinueAttack(game, npc).Justification;
        Own(game, "C2", "P1");
        var balancing = _strategy.DecideTransferAfterConquest(game, npc, "C1", "C2")!.Justification;
        var weakBoard = GameBuilder.Create(3, 1, 3);
        var holding = _strategy.DecideAttack(weakBoard, weakBoard.GetPlayer("P1")).Justification;

        Assert.All(
            new[] { reinforcement, attack.Justification, keepRolling, balancing, holding },
            justification => Assert.Matches(@"\d", justification));
        Assert.Contains("% win chance", attack.Justification);
    }
}
