using GeoConquest.Core.Enums;
using GeoConquest.Core.Exceptions;
using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;
using GeoConquest.Core.Services;
using GeoConquest.Core.Tests.Fakes;
using NSubstitute;

namespace GeoConquest.Core.Tests.Services;

public sealed class NpcTurnProcessorTests
{
    private static readonly NpcPacingOptions Pacing = new() { NormalDelayMilliseconds = 1200, FastForwardDelayMilliseconds = 200 };

    private readonly IGameRepository _gameRepository = Substitute.For<IGameRepository>();
    private readonly IGameEventRepository _gameEventRepository = Substitute.For<IGameEventRepository>();
    private readonly IGameUpdatePublisher _publisher = Substitute.For<IGameUpdatePublisher>();
    private readonly RecordingTimeProvider _timeProvider = new();
    private readonly NpcTurnProcessor _processor;

    public NpcTurnProcessorTests()
    {
        var engine = new GameEngine(
            Substitute.For<IGameSetupService>(),
            new BattleService(new FixedDiceRoller()),
            new ReinforcementService(),
            new FixedTimeProvider(GameBuilder.Now));

        _processor = new NpcTurnProcessor(
            new NpcTurnExecutor(engine, new PassiveNpcStrategy()),
            _gameRepository,
            new GameProgressRecorder(
                _gameRepository,
                _gameEventRepository,
                _publisher,
                new BattleCommentaryScheduler(Substitute.For<ICommentaryQueue>(), new CommentaryOptions(), _timeProvider)),
            Pacing,
            _timeProvider);
    }

    /// <summary>Three players; the human (P0) has just skipped, so P1 and then P2 are the NPCs still to play before round 2.</summary>
    private static Game GameWithTwoNpcTurnsToPlay(bool fastForward = false)
    {
        var game = GameBuilder.Create(5, 5, 5);
        game.AdvanceToNextLivingPlayer();
        game.StartTurn(0);
        if (fastForward)
        {
            game.SetFastForward(true);
        }

        return game;
    }

    [Fact]
    public async Task PlayUntilHumanTurnAsync_NpcsToPlay_SavesAppendsAndPublishesEveryStepBeforeTheNext()
    {
        var game = GameWithTwoNpcTurnsToPlay();
        _gameRepository.GetByIdAsync(game.Id, Arg.Any<CancellationToken>()).Returns(game);

        await _processor.PlayUntilHumanTurnAsync(game.Id, CancellationToken.None);

        Assert.True(game.CurrentPlayer.IsHuman);
        Received.InOrder(() =>
        {
            _gameRepository.UpdateAsync(game, Arg.Any<CancellationToken>());
            _gameEventRepository.AppendAsync(Arg.Any<IReadOnlyCollection<GameEvent>>(), Arg.Any<CancellationToken>());
            _publisher.PublishAsync(game, Arg.Any<IReadOnlyList<GameEvent>>(), Arg.Any<CancellationToken>());
            _gameRepository.UpdateAsync(game, Arg.Any<CancellationToken>());
            _gameEventRepository.AppendAsync(Arg.Any<IReadOnlyCollection<GameEvent>>(), Arg.Any<CancellationToken>());
            _publisher.PublishAsync(game, Arg.Any<IReadOnlyList<GameEvent>>(), Arg.Any<CancellationToken>());
        });
        await _gameRepository.Received(2).UpdateAsync(game, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlayUntilHumanTurnAsync_HumansTurn_ChangesAndPublishesNothing()
    {
        var game = GameBuilder.Create();
        _gameRepository.GetByIdAsync(game.Id, Arg.Any<CancellationToken>()).Returns(game);

        await _processor.PlayUntilHumanTurnAsync(game.Id, CancellationToken.None);

        await _gameRepository.DidNotReceive().UpdateAsync(Arg.Any<Game>(), Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().PublishAsync(Arg.Any<Game>(), Arg.Any<IReadOnlyList<GameEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlayUntilHumanTurnAsync_GameSavedElsewhereMidStep_ReplaysTheStepWithoutPublishingTheLostOne()
    {
        // Each reload is a fresh copy, as from the database: the first two are NPC turns, the third is the human's.
        _gameRepository
            .GetByIdAsync("game-1", Arg.Any<CancellationToken>())
            .Returns(_ => GameWithTwoNpcTurnsToPlay(), _ => GameWithTwoNpcTurnsToPlay(), _ => GameBuilder.Create());
        _gameRepository
            .UpdateAsync(Arg.Any<Game>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ConcurrencyException("changed")), Task.CompletedTask);

        await _processor.PlayUntilHumanTurnAsync("game-1", CancellationToken.None);

        await _gameRepository.Received(2).UpdateAsync(Arg.Any<Game>(), Arg.Any<CancellationToken>());
        await _publisher.Received(1).PublishAsync(Arg.Any<Game>(), Arg.Any<IReadOnlyList<GameEvent>>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(false, 1200)]
    [InlineData(true, 200)]
    public async Task PlayUntilHumanTurnAsync_FastForward_ChangesOnlyTheWaitBetweenSteps(bool fastForward, int expectedDelayMilliseconds)
    {
        var game = GameWithTwoNpcTurnsToPlay(fastForward);
        _gameRepository.GetByIdAsync(game.Id, Arg.Any<CancellationToken>()).Returns(game);
        var published = new List<GameEventType>();
        _publisher
            .When(publisher => publisher.PublishAsync(game, Arg.Any<IReadOnlyList<GameEvent>>(), Arg.Any<CancellationToken>()))
            .Do(call => published.AddRange(call.Arg<IReadOnlyList<GameEvent>>().Select(gameEvent => gameEvent.Type)));

        await _processor.PlayUntilHumanTurnAsync(game.Id, CancellationToken.None);

        // One wait only: between the two NPC steps, none after the last one.
        Assert.Equal([TimeSpan.FromMilliseconds(expectedDelayMilliseconds)], _timeProvider.Delays);
        Assert.Equal(
            [
                GameEventType.NpcDecision, GameEventType.TurnSkipped, GameEventType.TurnStarted,
                GameEventType.NpcDecision, GameEventType.TurnSkipped, GameEventType.TurnStarted,
            ],
            published);
    }
}
