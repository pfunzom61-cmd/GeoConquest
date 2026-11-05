using GeoConquest.Core.Enums;
using GeoConquest.Core.Exceptions;
using GeoConquest.Core.Models;
using GeoConquest.Core.Tests.Fakes;

namespace GeoConquest.Core.Tests.Models;

public sealed class GameTests
{
    [Fact]
    public void StartTurn_TurnReachesTheHuman_SwitchesFastForwardOff()
    {
        var game = GameBuilder.Create(5, 5, 5);
        game.AdvanceToNextLivingPlayer();
        game.SetFastForward(true);
        game.StartTurn(0);
        Assert.True(game.IsFastForward);

        game.AdvanceToNextLivingPlayer();
        game.AdvanceToNextLivingPlayer();
        game.StartTurn(0);

        Assert.True(game.CurrentPlayer.IsHuman);
        Assert.False(game.IsFastForward);
    }

    [Fact]
    public void SetFastForward_GameOver_IsRejected()
    {
        var game = GameBuilder.Create(5, 5);
        game.Finish(GameStatus.Lost, winnerPlayerId: null);

        Assert.Throws<GameRuleException>(() => game.SetFastForward(true));
    }

    [Fact]
    public void AdvanceToNextLivingPlayer_WrappingTurnOrder_StartsNextRound()
    {
        var game = GameBuilder.Create(5, 5, 5);

        game.AdvanceToNextLivingPlayer();
        game.AdvanceToNextLivingPlayer();
        Assert.Equal(1, game.RoundNumber);

        game.AdvanceToNextLivingPlayer();

        Assert.Equal(2, game.RoundNumber);
        Assert.Equal("P0", game.CurrentPlayer.Id);
    }

    [Fact]
    public void AdvanceToNextLivingPlayer_EliminatedPlayerNext_SkipsThem()
    {
        var game = GameBuilder.Create(5, 5, 5);
        game.GetPlayer("P1").Eliminate();

        game.AdvanceToNextLivingPlayer();

        Assert.Equal("P2", game.CurrentPlayer.Id);
    }

    [Fact]
    public void AdvanceToNextLivingPlayer_EliminatedFirstPlayer_StillCountsRoundOnWrap()
    {
        var game = GameBuilder.Create(5, 5, 5);
        game.GetPlayer("P0").Eliminate();
        game.AdvanceToNextLivingPlayer();
        game.AdvanceToNextLivingPlayer();
        Assert.Equal("P2", game.CurrentPlayer.Id);

        game.AdvanceToNextLivingPlayer();

        Assert.Equal("P1", game.CurrentPlayer.Id);
        Assert.Equal(2, game.RoundNumber);
    }

    [Fact]
    public void StartTurn_WithReinforcements_EntersReinforcePhase()
    {
        var game = GameBuilder.Create();

        game.StartTurn(reinforcements: 6);

        Assert.Equal(TurnPhase.Reinforce, game.Phase);
        Assert.Equal(6, game.PendingReinforcements);
    }

    [Fact]
    public void BeginBattle_OutsideActionPhase_Throws()
    {
        var game = GameBuilder.Create();
        game.StartTurn(reinforcements: 2);

        Assert.Throws<GameRuleException>(() => game.BeginBattle("C0", "C1"));
    }

    [Fact]
    public void NextEventSequence_CalledRepeatedly_IncreasesByOne()
    {
        var game = GameBuilder.Create();

        Assert.Equal(1, game.NextEventSequence());
        Assert.Equal(2, game.NextEventSequence());
    }

    [Fact]
    public void Constructor_TurnOrderMissingAPlayer_Throws()
    {
        var game = GameBuilder.Create();
        var players = game.Players.ToList();

        Assert.Throws<ArgumentException>(() => new Game(
            "g", "u", "Bob", players, game.Territories, players.Take(2).Select(player => player.Id), GameBuilder.Now));
    }
}
