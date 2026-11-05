using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;
using GeoConquest.Core.Services;
using GeoConquest.Core.Tests.Fakes;
using NSubstitute;

namespace GeoConquest.Core.Tests.Services;

public sealed class CommentaryPromptBuilderTests
{
    [Fact]
    public void Build_BattleFacts_StatesWhoRolledWhatWhoLostASoldierAndWhoIsAhead()
    {
        var prompt = CommentaryPromptBuilder.Build(BattleContexts.BrazilAttacksChile);

        Assert.Contains("Brazil is attacking Chile.", prompt);
        Assert.Contains("Brazil's die showed 6 against Chile's die showing 5.", prompt);
        Assert.Contains("Brazil's die showed 2 against Chile's die showing 3.", prompt);
        Assert.Contains("In this roll, Brazil lost a soldier and Chile lost a soldier.", prompt);
        Assert.Contains("Overall in this attack, Brazil is ahead because it has lost fewer soldiers than Chile.", prompt);
        Assert.Contains("The attack goes on.", prompt);
    }

    [Fact]
    public void Build_RoundNumbers_AreLeftOut()
    {
        var prompt = CommentaryPromptBuilder.Build(BattleContexts.BrazilAttacksChile with { BattleRoundNumber = 37, GameRoundNumber = 91 });

        Assert.DoesNotContain("37", prompt);
        Assert.DoesNotContain("91", prompt);
    }

    [Fact]
    public void Build_LargeRoll_ListsOnlyTheCappedDicePairs()
    {
        var dice = Enumerable.Repeat(4, 50).ToList();

        var prompt = CommentaryPromptBuilder.Build(BattleContexts.BrazilAttacksChile with { AttackerDice = dice, DefenderDice = dice });

        Assert.Equal(3, prompt.Split('\n').Count(line => line.Contains("die showed", StringComparison.Ordinal)));
        Assert.Contains("More dice were rolled as well.", prompt);
    }

    [Fact]
    public void Build_NextRoundOfTheSameBattle_AsksForADifferentStyle()
    {
        var round = BattleContexts.BrazilAttacksChile;

        var style = StyleLine(CommentaryPromptBuilder.Build(round));
        var nextStyle = StyleLine(CommentaryPromptBuilder.Build(round with { BattleRoundNumber = round.BattleRoundNumber + 1 }));

        Assert.NotEqual(style, nextStyle);
    }

    [Fact]
    public void Build_FromARealRoll_NeverIncludesPlayerNamesOrAccountDetails()
    {
        var game = GameBuilder.Create(3, 5);
        var engine = new GameEngine(
            Substitute.For<IGameSetupService>(),
            new BattleService(new FixedDiceRoller([6, 1], [1, 6])),
            new ReinforcementService(),
            new FixedTimeProvider(GameBuilder.Now));
        engine.StartAttack(game, "P0", "C0", "C1");
        var roll = engine.RollAttack(game, "P0").Single();

        var prompt = CommentaryPromptBuilder.Build(BattleContext.FromRoll(game, roll));

        Assert.DoesNotContain(game.OwnerDisplayName, prompt);
        Assert.DoesNotContain(game.OwnerUserId, prompt);
        Assert.All(game.Players, player => Assert.DoesNotContain(player.DisplayName, prompt));
    }

    private static string StyleLine(string prompt) =>
        prompt.Split('\n').Single(line => line.StartsWith("Style:", StringComparison.Ordinal));
}
