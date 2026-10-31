using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;
using GeoConquest.Core.Services;
using GeoConquest.Core.Tests.Fakes;
using NSubstitute;

namespace GeoConquest.Core.Tests.Services;

public sealed class CommentaryPromptBuilderTests
{
    [Fact]
    public void Build_BattleFacts_IncludesCountriesSoldiersDiceAndRound()
    {
        var prompt = CommentaryPromptBuilder.Build(BattleContexts.BrazilAttacksChile);

        Assert.Contains("Attacking country: Brazil", prompt);
        Assert.Contains("Defending country: Chile", prompt);
        Assert.Contains("Attacking soldiers: 8 before, 7 after", prompt);
        Assert.Contains("Defending soldiers: 3 before, 2 after", prompt);
        Assert.Contains("Attacker dice: 6, 2", prompt);
        Assert.Contains("Defender dice: 5, 3", prompt);
        Assert.Contains("Attack round: 3", prompt);
        Assert.Contains("Momentum so far: attacker ahead", prompt);
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
