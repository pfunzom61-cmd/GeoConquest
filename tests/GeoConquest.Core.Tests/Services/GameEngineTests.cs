using GeoConquest.Core.Enums;
using GeoConquest.Core.Exceptions;
using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;
using GeoConquest.Core.Services;
using GeoConquest.Core.Tests.Fakes;
using NSubstitute;

namespace GeoConquest.Core.Tests.Services;

public sealed class GameEngineTests
{
    private static readonly int[] OneDieWinForAttacker = [6];
    private static readonly int[] OneDieLossForDefender = [1];

    /// <summary>Engine with real rules and scripted dice (attacker dice, then defender dice, for each round rolled).</summary>
    private static GameEngine CreateEngine(params int[][] diceScript) =>
        CreateEngineWithSetup(Substitute.For<IGameSetupService>(), diceScript);

    private static GameEngine CreateEngineWithSetup(IGameSetupService setupService, params int[][] diceScript) => new(
        setupService,
        new BattleService(new FixedDiceRoller(diceScript)),
        new ReinforcementService(),
        new FixedTimeProvider(GameBuilder.Now));

    /// <summary>One roll in which the attacker's single die beats the defender's, so a 1-soldier defender is conquered.</summary>
    private static int[][] AttackerWinsOneDieRoll() => [OneDieWinForAttacker, OneDieLossForDefender];

    private static void GiveCountry(Game game, string countryCode, string newOwnerId) =>
        game.GetTerritory(countryCode).ChangeOwner(newOwnerId, game.GetTerritory(countryCode).Soldiers);

    // ---- Game creation ----

    [Fact]
    public void CreateGame_ValidRequest_StartsRoundOneInActionPhaseWithNoReinforcements()
    {
        var setupService = Substitute.For<IGameSetupService>();
        setupService.CreateGame("user-1", "Bob", "C0", Arg.Any<IReadOnlyList<Country>>()).Returns(GameBuilder.Create());
        var engine = CreateEngineWithSetup(setupService);

        var result = engine.CreateGame("user-1", "Bob", "C0", []);

        Assert.Equal(1, result.Game.RoundNumber);
        Assert.Equal(TurnPhase.Action, result.Game.Phase);
        Assert.Equal(0, result.Game.PendingReinforcements);
        Assert.Equal([GameEventType.GameCreated, GameEventType.TurnStarted], result.Events.Select(@event => @event.Type));
    }

    // ---- Turn flow and reinforcements ----

    [Fact]
    public void Skip_EveryoneHasHadATurn_StartsRoundTwoWithTwoReinforcementsPerCountry()
    {
        var game = GameBuilder.Create(5, 5, 5, 5);
        GiveCountry(game, "C1", "P0");
        GiveCountry(game, "C2", "P0");
        game.GetPlayer("P1").Eliminate();
        game.GetPlayer("P2").Eliminate();
        var engine = CreateEngine();

        engine.Skip(game, "P0");
        var events = engine.Skip(game, "P3");

        Assert.Equal(2, game.RoundNumber);
        Assert.Equal("P0", game.CurrentPlayer.Id);
        Assert.Equal(TurnPhase.Reinforce, game.Phase);
        Assert.Equal(6, game.PendingReinforcements);
        Assert.Contains("6 reinforcements", events[^1].Message);
    }

    [Fact]
    public void Skip_RoundOne_NextPlayerReceivesNoReinforcements()
    {
        var game = GameBuilder.Create();

        CreateEngine().Skip(game, "P0");

        Assert.Equal("P1", game.CurrentPlayer.Id);
        Assert.Equal(TurnPhase.Action, game.Phase);
        Assert.Equal(0, game.PendingReinforcements);
    }

    [Fact]
    public void AllocateReinforcements_ValidSplit_LogsEachPlacementAndOpensActionPhase()
    {
        var game = GameBuilder.Create(5, 5, 5);
        GiveCountry(game, "C1", "P0");
        game.StartTurn(reinforcements: 4);

        var events = CreateEngine().AllocateReinforcements(
            game, "P0", new Dictionary<string, int> { ["C0"] = 3, ["C1"] = 1 });

        Assert.Equal(2, events.Count);
        Assert.All(events, @event => Assert.Equal(GameEventType.Reinforced, @event.Type));
        Assert.Equal(TurnPhase.Action, game.Phase);
        Assert.Equal(8, game.GetTerritory("C0").Soldiers);
    }

    [Fact]
    public void StartAttack_ReinforcementsNotPlacedYet_Throws()
    {
        var game = GameBuilder.Create();
        game.StartTurn(reinforcements: 2);

        Assert.Throws<GameRuleException>(() => CreateEngine().StartAttack(game, "P0", "C0", "C1"));
    }

    [Fact]
    public void Skip_ReinforcementsNotPlacedYet_Throws()
    {
        var game = GameBuilder.Create();
        game.StartTurn(reinforcements: 2);

        Assert.Throws<GameRuleException>(() => CreateEngine().Skip(game, "P0"));
    }

    // ---- Transfers ----

    [Fact]
    public void Transfer_BetweenOwnCountries_MovesSoldiersAndLogsIt()
    {
        var game = GameBuilder.Create(5, 5, 5);
        GiveCountry(game, "C1", "P0");

        var events = CreateEngine().Transfer(game, "P0", "C0", "C1", 4);

        Assert.Equal(1, game.GetTerritory("C0").Soldiers);
        Assert.Equal(9, game.GetTerritory("C1").Soldiers);
        Assert.Equal(GameEventType.Transferred, Assert.Single(events).Type);
    }

    [Fact]
    public void Transfer_WouldLeaveCountryEmpty_Throws()
    {
        var game = GameBuilder.Create(5, 5, 5);
        GiveCountry(game, "C1", "P0");

        Assert.Throws<GameRuleException>(() => CreateEngine().Transfer(game, "P0", "C0", "C1", 5));
        Assert.Equal(5, game.GetTerritory("C0").Soldiers);
    }

    [Fact]
    public void Transfer_ToOrFromEnemyCountry_Throws()
    {
        var game = GameBuilder.Create(5, 5, 5);
        GiveCountry(game, "C1", "P0");
        var engine = CreateEngine();

        Assert.Throws<GameRuleException>(() => engine.Transfer(game, "P0", "C0", "C2", 1));
        Assert.Throws<GameRuleException>(() => engine.Transfer(game, "P0", "C2", "C0", 1));
    }

    [Fact]
    public void Transfer_SameCountry_Throws()
    {
        var game = GameBuilder.Create();

        Assert.Throws<GameRuleException>(() => CreateEngine().Transfer(game, "P0", "C0", "C0", 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Transfer_NonPositiveAmount_Throws(int soldiers)
    {
        var game = GameBuilder.Create(5, 5, 5);
        GiveCountry(game, "C1", "P0");

        Assert.Throws<GameRuleException>(() => CreateEngine().Transfer(game, "P0", "C0", "C1", soldiers));
    }

    [Fact]
    public void Transfer_WhileAttackInProgress_Throws()
    {
        var game = GameBuilder.Create(5, 5, 5);
        GiveCountry(game, "C2", "P0");
        var engine = CreateEngine();
        engine.StartAttack(game, "P0", "C0", "C1");

        Assert.Throws<GameRuleException>(() => engine.Transfer(game, "P0", "C0", "C2", 1));
    }

    [Fact]
    public void Transfer_BeforeReinforcementsPlaced_Throws()
    {
        var game = GameBuilder.Create(5, 5, 5);
        GiveCountry(game, "C1", "P0");
        game.StartTurn(reinforcements: 2);

        Assert.Throws<GameRuleException>(() => CreateEngine().Transfer(game, "P0", "C0", "C1", 1));
    }

    [Fact]
    public void Transfer_AfterConquest_IsAllowed()
    {
        var game = GameBuilder.Create(5, 1, 5, 5);
        var engine = CreateEngine(AttackerWinsOneDieRoll());
        engine.StartAttack(game, "P0", "C0", "C1");
        engine.RollAttack(game, "P0");

        engine.Transfer(game, "P0", "C0", "C1", 3);

        Assert.Equal(2, game.GetTerritory("C0").Soldiers);
        Assert.Equal(5, game.GetTerritory("C1").Soldiers);
    }

    [Fact]
    public void Transfer_AfterFailedAttack_IsAllowed()
    {
        var game = GameBuilder.Create(2, 5, 5);
        GiveCountry(game, "C2", "P0");
        var engine = CreateEngine([1], [6]);
        engine.StartAttack(game, "P0", "C0", "C1");
        engine.RollAttack(game, "P0");

        engine.Transfer(game, "P0", "C2", "C0", 2);

        Assert.Equal(3, game.GetTerritory("C0").Soldiers);
    }

    // ---- Starting and rolling attacks ----

    [Fact]
    public void StartAttack_EnemyTargetAndTwoSoldiers_EntersAttackingPhase()
    {
        var game = GameBuilder.Create(2, 5);

        var events = CreateEngine().StartAttack(game, "P0", "C0", "C1");

        Assert.Equal(TurnPhase.Attacking, game.Phase);
        Assert.Equal(GameEventType.AttackStarted, Assert.Single(events).Type);
    }

    [Fact]
    public void StartAttack_AttackerHasOneSoldier_Throws()
    {
        var game = GameBuilder.Create(1, 5);

        Assert.Throws<GameRuleException>(() => CreateEngine().StartAttack(game, "P0", "C0", "C1"));
    }

    [Fact]
    public void StartAttack_OwnCountryAsTarget_Throws()
    {
        var game = GameBuilder.Create(5, 5, 5);
        GiveCountry(game, "C1", "P0");

        Assert.Throws<GameRuleException>(() => CreateEngine().StartAttack(game, "P0", "C0", "C1"));
    }

    [Fact]
    public void StartAttack_FromEnemyCountry_Throws()
    {
        var game = GameBuilder.Create();

        Assert.Throws<GameRuleException>(() => CreateEngine().StartAttack(game, "P0", "C1", "C2"));
    }

    [Fact]
    public void StartAttack_SecondAttackInSameTurn_Throws()
    {
        var game = GameBuilder.Create(2, 5, 5);
        GiveCountry(game, "C2", "P0");
        var engine = CreateEngine([1], [6]);
        engine.StartAttack(game, "P0", "C0", "C1");
        engine.RollAttack(game, "P0");

        Assert.Throws<GameRuleException>(() => engine.StartAttack(game, "P0", "C2", "C1"));
    }

    [Fact]
    public void StartAttack_FromCountryConqueredThisTurn_ThrowsButSucceedsNextTurn()
    {
        var game = GameBuilder.Create(5, 1, 5, 5);
        var engine = CreateEngine(AttackerWinsOneDieRoll());
        engine.StartAttack(game, "P0", "C0", "C1");
        engine.RollAttack(game, "P0");
        Assert.Equal("P0", game.GetTerritory("C1").OwnerPlayerId);
        Assert.Equal(2, game.GetTerritory("C1").Soldiers);

        // This turn: the conquered country (and every other) is locked out because the turn's one attack is used.
        Assert.Throws<GameRuleException>(() => engine.StartAttack(game, "P0", "C1", "C2"));

        // P1 was eliminated by the conquest and is skipped; P2 and P3 skip; then it is P0's round 2.
        engine.EndTurn(game, "P0");
        engine.Skip(game, "P2");
        engine.Skip(game, "P3");
        engine.AllocateReinforcements(game, "P0", new Dictionary<string, int> { ["C0"] = 4 });

        engine.StartAttack(game, "P0", "C1", "C2");

        Assert.Equal(TurnPhase.Attacking, game.Phase);
        Assert.Equal("C1", game.ActiveBattle!.AttackerCountryCode);
    }

    [Fact]
    public void RollAttack_RoundWithNoDecision_KeepsAttackGoing()
    {
        var game = GameBuilder.Create(3, 5);
        var engine = CreateEngine([3, 3], [3, 3]);
        engine.StartAttack(game, "P0", "C0", "C1");

        var events = engine.RollAttack(game, "P0");

        Assert.Equal(TurnPhase.Attacking, game.Phase);
        Assert.Equal(GameEventType.DiceRolled, Assert.Single(events).Type);
    }

    [Fact]
    public void RollAttack_AttackerFallsToOneSoldier_AttackFailsAndTurnAwaitsEnd()
    {
        var game = GameBuilder.Create(2, 5, 5);
        var engine = CreateEngine([1], [6]);
        engine.StartAttack(game, "P0", "C0", "C1");

        var events = engine.RollAttack(game, "P0");

        Assert.Equal([GameEventType.DiceRolled, GameEventType.AttackFailed], events.Select(@event => @event.Type));
        Assert.Equal(TurnPhase.PostAttack, game.Phase);
        Assert.Equal("P1", game.GetTerritory("C1").OwnerPlayerId);
    }

    [Fact]
    public void RollAttack_DefenderWipedOut_ConquersAndAwaitsEndOfTurn()
    {
        var game = GameBuilder.Create(5, 1, 5, 5);
        var engine = CreateEngine(AttackerWinsOneDieRoll());
        engine.StartAttack(game, "P0", "C0", "C1");

        var events = engine.RollAttack(game, "P0");

        Assert.Contains(events, @event => @event.Type == GameEventType.CountryConquered);
        Assert.Equal(TurnPhase.PostAttack, game.Phase);
        Assert.Equal("P0", game.GetTerritory("C1").OwnerPlayerId);
    }

    [Fact]
    public void StopAttack_AttackInProgress_EndsTurnImmediately()
    {
        var game = GameBuilder.Create();
        var engine = CreateEngine();
        engine.StartAttack(game, "P0", "C0", "C1");

        var events = engine.StopAttack(game, "P0");

        Assert.Equal([GameEventType.AttackStopped, GameEventType.TurnStarted], events.Select(@event => @event.Type));
        Assert.Equal("P1", game.CurrentPlayer.Id);
        Assert.Equal(TurnPhase.Action, game.Phase);
        Assert.Null(game.ActiveBattle);
    }

    // ---- Phase and actor guards ----

    [Fact]
    public void Skip_AfterAttackResolved_Throws()
    {
        var game = GameBuilder.Create(2, 5, 5);
        var engine = CreateEngine([1], [6]);
        engine.StartAttack(game, "P0", "C0", "C1");
        engine.RollAttack(game, "P0");

        Assert.Throws<GameRuleException>(() => engine.Skip(game, "P0"));
    }

    [Fact]
    public void EndTurn_BeforeAnyAttack_Throws()
    {
        var game = GameBuilder.Create();

        Assert.Throws<GameRuleException>(() => CreateEngine().EndTurn(game, "P0"));
    }

    [Fact]
    public void StopAttackAndRollAttack_WithNoAttackInProgress_Throw()
    {
        var game = GameBuilder.Create();
        var engine = CreateEngine();

        Assert.Throws<GameRuleException>(() => engine.StopAttack(game, "P0"));
        Assert.Throws<GameRuleException>(() => engine.RollAttack(game, "P0"));
    }

    [Fact]
    public void Skip_DuringAttack_Throws()
    {
        var game = GameBuilder.Create();
        var engine = CreateEngine();
        engine.StartAttack(game, "P0", "C0", "C1");

        Assert.Throws<GameRuleException>(() => engine.Skip(game, "P0"));
    }

    [Fact]
    public void Skip_PlayerWhoseTurnItIsNot_Throws()
    {
        var game = GameBuilder.Create();

        var exception = Assert.Throws<GameRuleException>(() => CreateEngine().Skip(game, "P1"));

        Assert.Equal("It is not your turn.", exception.Message);
    }

    [Fact]
    public void Skip_UnknownPlayer_Throws()
    {
        Assert.Throws<GameRuleException>(() => CreateEngine().Skip(GameBuilder.Create(), "nobody"));
    }

    // ---- Elimination, winning and losing ----

    [Fact]
    public void RollAttack_ConquestTakesLastCountry_EliminatesPlayerAndTheirTurnsAreSkipped()
    {
        var game = GameBuilder.Create(5, 1, 5, 5);
        var engine = CreateEngine(AttackerWinsOneDieRoll());
        engine.StartAttack(game, "P0", "C0", "C1");

        var events = engine.RollAttack(game, "P0");
        engine.EndTurn(game, "P0");

        Assert.Contains(events, @event => @event.Type == GameEventType.PlayerEliminated && @event.ActorPlayerId == "P1");
        Assert.True(game.GetPlayer("P1").IsEliminated);
        Assert.Equal("P2", game.CurrentPlayer.Id);
        Assert.Equal(GameStatus.InProgress, game.Status);
    }

    [Fact]
    public void RollAttack_HumanConquersLastEnemyCountry_GameIsWon()
    {
        var game = GameBuilder.Create(5, 1);
        var engine = CreateEngine(AttackerWinsOneDieRoll());
        engine.StartAttack(game, "P0", "C0", "C1");

        var events = engine.RollAttack(game, "P0");

        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal("P0", game.WinnerPlayerId);
        Assert.Equal(TurnPhase.GameOver, game.Phase);
        Assert.Contains(events, @event => @event.Type == GameEventType.GameWon);
        Assert.Throws<GameRuleException>(() => engine.Skip(game, "P0"));
    }

    [Fact]
    public void RollAttack_NpcEliminatesHumanWhileOthersRemain_GameIsLostWithNoWinner()
    {
        var game = GameBuilder.Create(1, 5, 5, 5);
        var engine = CreateEngine(AttackerWinsOneDieRoll());
        engine.Skip(game, "P0");
        engine.StartAttack(game, "P1", "C1", "C0");

        var events = engine.RollAttack(game, "P1");

        Assert.Equal(GameStatus.Lost, game.Status);
        Assert.Null(game.WinnerPlayerId);
        Assert.Contains(events, @event => @event.Type == GameEventType.GameLost);
    }

    [Fact]
    public void RollAttack_NpcOwnsAllTenCountries_GameIsLostAndNpcIsTheWinner()
    {
        var game = GameBuilder.Create(1, 5);
        var engine = CreateEngine(AttackerWinsOneDieRoll());
        engine.Skip(game, "P0");
        engine.StartAttack(game, "P1", "C1", "C0");

        engine.RollAttack(game, "P1");

        Assert.Equal(GameStatus.Lost, game.Status);
        Assert.Equal("P1", game.WinnerPlayerId);
        Assert.Equal(TurnPhase.GameOver, game.Phase);
    }

    // ---- Event log ----

    [Fact]
    public void Actions_AcrossAWholeTurn_ProduceStrictlyIncreasingEventSequenceNumbers()
    {
        var setupService = Substitute.For<IGameSetupService>();
        setupService.CreateGame(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<Country>>())
            .Returns(GameBuilder.Create(5, 1, 5));
        var engine = CreateEngineWithSetup(setupService, AttackerWinsOneDieRoll());

        var creation = engine.CreateGame("user-1", "Bob", "C0", []);
        var game = creation.Game;
        var events = creation.Events
            .Concat(engine.StartAttack(game, "P0", "C0", "C1"))
            .Concat(engine.RollAttack(game, "P0"))
            .Concat(engine.EndTurn(game, "P0"))
            .ToList();

        Assert.Equal(Enumerable.Range(1, events.Count), events.Select(@event => @event.Sequence));
    }

    [Fact]
    public void RecordNpcDecision_NpcsTurn_LogsTheJustificationWithoutChangingTheGame()
    {
        var game = GameBuilder.Create();
        game.AdvanceToNextLivingPlayer();
        var engine = CreateEngine();

        var decision = engine.RecordNpcDecision(game, "P1", "Attacking Country 2 from Country 1 (5 vs 2): 93% win chance, +2 soldiers.");

        Assert.Equal(GameEventType.NpcDecision, decision.Type);
        Assert.Equal("P1", decision.ActorPlayerId);
        Assert.Equal(game.LastEventSequence, decision.Sequence);
        Assert.Equal(TurnPhase.Action, game.Phase);
    }
}
