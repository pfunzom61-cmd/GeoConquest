using GeoConquest.Core.Enums;
using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;
using GeoConquest.Core.Services;
using GeoConquest.Core.Tests.Fakes;
using NSubstitute;

namespace GeoConquest.Core.Tests.Services;

public sealed class BattleCommentaryTests
{
    private const int BudgetMilliseconds = 2500;

    private readonly ICommentaryQueue _queue = Substitute.For<ICommentaryQueue>();
    private readonly IGameRepository _gameRepository = Substitute.For<IGameRepository>();
    private readonly IGameEventRepository _gameEventRepository = Substitute.For<IGameEventRepository>();
    private readonly IGameUpdatePublisher _publisher = Substitute.For<IGameUpdatePublisher>();
    private readonly FixedTimeProvider _clock = new(GameBuilder.Now);

    private BattleCommentaryScheduler CreateScheduler() =>
        new(_queue, new CommentaryOptions { TimeBudgetMilliseconds = BudgetMilliseconds }, _clock);

    private static IReadOnlyList<GameEvent> RollDice(Game game, string playerId, string attacker, string defender)
    {
        var engine = new GameEngine(
            Substitute.For<IGameSetupService>(),
            new BattleService(new FixedDiceRoller([6, 1], [1, 6])),
            new ReinforcementService(),
            new FixedTimeProvider(GameBuilder.Now));
        engine.StartAttack(game, playerId, attacker, defender);
        return engine.RollAttack(game, playerId);
    }

    [Theory]
    [InlineData(true, false, BudgetMilliseconds)]
    [InlineData(true, true, BudgetMilliseconds)]
    [InlineData(false, false, BudgetMilliseconds)]
    [InlineData(false, true, 0)]
    public void ScheduleFor_DiceRound_GivesEveryRollTheFullBudgetExceptFastForwardedNpcRolls(bool humanRolls, bool fastForward, int expectedBudgetMilliseconds)
    {
        var game = GameBuilder.Create(3, 3);
        if (!humanRolls)
        {
            game.AdvanceToNextLivingPlayer();
        }

        game.SetFastForward(fastForward);
        var events = humanRolls ? RollDice(game, "P0", "C0", "C1") : RollDice(game, "P1", "C1", "C0");

        CreateScheduler().ScheduleFor(game, events);

        var roll = events.Single(gameEvent => gameEvent.Type == GameEventType.DiceRolled);
        _queue.Received(1).Enqueue(Arg.Is<CommentaryRequest>(request =>
            request.Sequence == roll.ReservedCommentarySequence
            && request.Deadline == GameBuilder.Now.AddMilliseconds(expectedBudgetMilliseconds)));
    }

    [Fact]
    public async Task ProcessAsync_QueuedRound_FillsTheReservedSlotAndPublishesWithoutSavingTheGame()
    {
        var recorder = new GameProgressRecorder(_gameRepository, _gameEventRepository, _publisher, CreateScheduler());
        var processor = new BattleCommentaryProcessor(new TemplateCommentaryService(), recorder, _clock);
        var request = new CommentaryRequest("game-1", 8, "P0", BattleContexts.BrazilAttacksChile, GameBuilder.Now);

        await processor.ProcessAsync(request, CancellationToken.None);

        await _gameEventRepository.Received(1).AppendAsync(
            Arg.Is<IReadOnlyCollection<GameEvent>>(events => IsTheCommentary(events.Single())), Arg.Any<CancellationToken>());
        await _publisher.Received(1).PublishEventsAsync(
            "game-1", Arg.Is<IReadOnlyList<GameEvent>>(events => IsTheCommentary(events.Single())), Arg.Any<CancellationToken>());
        await _gameRepository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }

    private static bool IsTheCommentary(GameEvent gameEvent) =>
        gameEvent.Sequence == 8
        && gameEvent.Type == GameEventType.Commentary
        && gameEvent.Payload!.CommentarySource == CommentarySource.Template;
}
