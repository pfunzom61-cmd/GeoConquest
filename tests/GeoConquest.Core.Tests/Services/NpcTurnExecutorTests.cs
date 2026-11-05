using GeoConquest.Core.Enums;
using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;
using GeoConquest.Core.Services;
using GeoConquest.Core.Tests.Fakes;
using NSubstitute;

namespace GeoConquest.Core.Tests.Services;

/// <summary>
/// Checks how the executor carries out a scripted strategy through the real engine: what it logs, in what order,
/// and when it ends the turn. P1 is the NPC and is made the current player by <see cref="StartNpcTurn"/>.
/// </summary>
public sealed class NpcTurnExecutorTests
{
    private static readonly AttackDecision HoldDecision = new(null, null, "Holding: best odds are 41% (5 vs 8), below the 60% needed.");
    private static readonly AttackDecision AttackC1OnC2 = new("C1", "C2", "Attacking Country 2 from Country 1: 93% win chance, +2 soldiers.");

    private readonly INpcStrategy _strategy = Substitute.For<INpcStrategy>();

    private NpcTurnExecutor CreateExecutor(params int[][] diceScript) => new(
        new GameEngine(
            Substitute.For<IGameSetupService>(),
            new BattleService(new FixedDiceRoller(diceScript)),
            new ReinforcementService(),
            new FixedTimeProvider(GameBuilder.Now)),
        _strategy);

    private static Game StartNpcTurn(Game game, int reinforcements = 0)
    {
        game.AdvanceToNextLivingPlayer();
        game.StartTurn(reinforcements);
        return game;
    }

    /// <summary>Plays steps until the NPC's turn is over (the next player is up) or the game ends, as the background runner does.</summary>
    private static List<GameEvent> PlayTurn(NpcTurnExecutor executor, Game game)
    {
        var npcId = game.CurrentPlayer.Id;
        var events = new List<GameEvent>();
        while (game.Status == GameStatus.InProgress && game.CurrentPlayer.Id == npcId)
        {
            events.AddRange(executor.PlayStep(game));
        }

        return events;
    }

    private static List<GameEventType> Types(IEnumerable<GameEvent> events) => events.Select(@event => @event.Type).ToList();

    [Fact]
    public void PlayStep_ReinforcementsOwed_LogsTheDecisionThenPlacesThem()
    {
        var game = StartNpcTurn(GameBuilder.Create(), reinforcements: 2);
        _strategy.DecideReinforcements(game, game.CurrentPlayer)
            .Returns(new ReinforcementDecision(new Dictionary<string, int> { ["C1"] = 2 }, "Placing 2 reinforcements: 2 in Country 1."));
        _strategy.DecideAttack(game, game.CurrentPlayer).Returns(HoldDecision);

        var events = PlayTurn(CreateExecutor(), game);

        Assert.Equal(
            [GameEventType.NpcDecision, GameEventType.Reinforced, GameEventType.NpcDecision, GameEventType.TurnSkipped, GameEventType.TurnStarted],
            Types(events));
        Assert.Equal(7, game.GetTerritory("C1").Soldiers);
    }

    [Fact]
    public void PlayStep_ReinforcementsOwed_PlacesThemInOneStepAndLeavesTheAttackForTheNext()
    {
        var game = StartNpcTurn(GameBuilder.Create(), reinforcements: 2);
        _strategy.DecideReinforcements(game, game.CurrentPlayer)
            .Returns(new ReinforcementDecision(new Dictionary<string, int> { ["C1"] = 2 }, "Placing 2 reinforcements: 2 in Country 1."));

        var events = CreateExecutor().PlayStep(game);

        Assert.Equal([GameEventType.NpcDecision, GameEventType.Reinforced], Types(events));
        Assert.Equal(TurnPhase.Action, game.Phase);
        Assert.Equal("P1", game.CurrentPlayer.Id);
    }

    [Fact]
    public void PlayStep_ANewExecutorForEveryStep_PlaysTheSameTurnAsOneExecutor()
    {
        var game = StartNpcTurn(GameBuilder.Create(), reinforcements: 2);
        _strategy.DecideReinforcements(game, game.CurrentPlayer)
            .Returns(new ReinforcementDecision(new Dictionary<string, int> { ["C1"] = 2 }, "Placing 2 reinforcements: 2 in Country 1."));
        _strategy.DecideAttack(game, game.CurrentPlayer).Returns(HoldDecision);
        var events = new List<GameEvent>();

        while (game.CurrentPlayer.Id == "P1")
        {
            events.AddRange(CreateExecutor().PlayStep(game));
        }

        Assert.Equal(
            [GameEventType.NpcDecision, GameEventType.Reinforced, GameEventType.NpcDecision, GameEventType.TurnSkipped, GameEventType.TurnStarted],
            Types(events));
    }

    [Fact]
    public void PlayStep_StrategyHolds_LogsTheReasonAndSkipsTheTurn()
    {
        var game = StartNpcTurn(GameBuilder.Create());
        _strategy.DecideAttack(game, game.CurrentPlayer).Returns(HoldDecision);

        var events = PlayTurn(CreateExecutor(), game);

        Assert.Equal([GameEventType.NpcDecision, GameEventType.TurnSkipped, GameEventType.TurnStarted], Types(events));
        Assert.Equal(HoldDecision.Justification, events[0].Message);
        Assert.Equal("P2", game.CurrentPlayer.Id);
    }

    [Fact]
    public void PlayStep_OddsCollapseMidAttack_CallsItOffAndEndsTheTurn()
    {
        var game = StartNpcTurn(GameBuilder.Create(3, 5, 5));
        _strategy.DecideAttack(game, game.CurrentPlayer).Returns(AttackC1OnC2);
        _strategy.DecideContinueAttack(game, game.CurrentPlayer).Returns(new ContinueAttackDecision(false, "Calling off the attack: 8% win chance."));
        int[][] oneRoundAttackerLosesOneSoldier = [[1, 3, 3, 3], [6, 3, 3, 3]];

        var events = PlayTurn(CreateExecutor(oneRoundAttackerLosesOneSoldier), game);

        Assert.Equal(
            [GameEventType.NpcDecision, GameEventType.AttackStarted, GameEventType.DiceRolled, GameEventType.NpcDecision, GameEventType.AttackStopped, GameEventType.TurnStarted],
            Types(events));
        Assert.Equal("P2", game.CurrentPlayer.Id);
    }

    [Fact]
    public void PlayStep_Conquest_BalancesTheCountriesThenEndsTheTurn()
    {
        var game = StartNpcTurn(GameBuilder.Create(3, 5, 1, 3));
        _strategy.DecideAttack(game, game.CurrentPlayer).Returns(AttackC1OnC2);
        _strategy.DecideTransferAfterConquest(game, game.CurrentPlayer, "C1", "C2")
            .Returns(new TransferDecision("C1", "C2", 1, "Moving 1 from Country 1 to Country 2 to balance them (5 vs 2 becomes 4 vs 3)."));
        int[][] attackerWinsTheOnlyDuel = [[6], [1]];

        var events = PlayTurn(CreateExecutor(attackerWinsTheOnlyDuel), game);

        Assert.Equal(
            [
                GameEventType.NpcDecision, GameEventType.AttackStarted, GameEventType.DiceRolled, GameEventType.CountryConquered,
                GameEventType.PlayerEliminated, GameEventType.NpcDecision, GameEventType.Transferred, GameEventType.TurnStarted,
            ],
            Types(events));
        Assert.Equal(("P1", 3, 4), (game.GetTerritory("C2").OwnerPlayerId, game.GetTerritory("C2").Soldiers, game.GetTerritory("C1").Soldiers));
    }

    [Fact]
    public void PlayStep_AttackFails_EndsTheTurnWithoutBalancing()
    {
        var game = StartNpcTurn(GameBuilder.Create(3, 2, 5));
        _strategy.DecideAttack(game, game.CurrentPlayer).Returns(AttackC1OnC2);
        int[][] attackerLosesTheOnlyDuel = [[1], [6]];

        var events = PlayTurn(CreateExecutor(attackerLosesTheOnlyDuel), game);

        Assert.Contains(GameEventType.AttackFailed, Types(events));
        Assert.Equal(GameEventType.TurnStarted, events[^1].Type);
        _strategy.DidNotReceiveWithAnyArgs().DecideTransferAfterConquest(default!, default!, default!, default!);
    }

    [Fact]
    public void PlayStep_ConquestWinsTheGame_StopsWithoutEndingTheTurn()
    {
        var game = StartNpcTurn(GameBuilder.Create(3, 5, 1));
        game.GetTerritory("C0").ChangeOwner("P1", 3); // the NPC already holds everything except Country 2
        _strategy.DecideAttack(game, game.CurrentPlayer).Returns(AttackC1OnC2);
        int[][] attackerWinsTheOnlyDuel = [[6], [1]];

        var events = PlayTurn(CreateExecutor(attackerWinsTheOnlyDuel), game);

        Assert.Equal(GameStatus.Lost, game.Status);
        Assert.Equal(GameEventType.GameLost, events[^1].Type);
        _strategy.DidNotReceiveWithAnyArgs().DecideTransferAfterConquest(default!, default!, default!, default!);
    }
}
