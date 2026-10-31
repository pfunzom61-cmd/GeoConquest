using GeoConquest.Core.Enums;
using GeoConquest.Core.Exceptions;
using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;
using GeoConquest.Core.Services;
using GeoConquest.Core.Tests.Fakes;
using NSubstitute;

namespace GeoConquest.Core.Tests.Services;

public sealed class GameApplicationServiceTests
{
    private readonly IGameRepository _gameRepository = Substitute.For<IGameRepository>();
    private readonly IGameEventRepository _gameEventRepository = Substitute.For<IGameEventRepository>();
    private readonly IGameUpdatePublisher _publisher = Substitute.For<IGameUpdatePublisher>();
    private readonly GameApplicationService _service;

    public GameApplicationServiceTests()
    {
        var engine = new GameEngine(
            Substitute.For<IGameSetupService>(),
            new BattleService(new FixedDiceRoller()),
            new ReinforcementService(),
            new FixedTimeProvider(GameBuilder.Now));

        _service = new GameApplicationService(
            engine,
            new NpcTurnExecutor(engine, new PassiveNpcStrategy()),
            new GameProgressRecorder(_gameRepository, _gameEventRepository, _publisher),
            Substitute.For<ICountryRepository>());
    }

    [Fact]
    public async Task SkipAsync_ValidTurn_SavesTheGameBeforeAppendingItsEvents()
    {
        var game = GameBuilder.Create();

        await _service.SkipAsync(game, CancellationToken.None);

        Received.InOrder(() =>
        {
            _gameRepository.UpdateAsync(game, Arg.Any<CancellationToken>());
            _gameEventRepository.AppendAsync(Arg.Any<IReadOnlyCollection<GameEvent>>(), Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task SkipAsync_ValidTurn_PublishesTheEventsOnlyAfterTheyAreStored()
    {
        var game = GameBuilder.Create();

        var events = await _service.SkipAsync(game, CancellationToken.None);

        Received.InOrder(() =>
        {
            _gameEventRepository.AppendAsync(Arg.Any<IReadOnlyCollection<GameEvent>>(), Arg.Any<CancellationToken>());
            _publisher.PublishAsync(game, events, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task SkipAsync_RequestCancelledOnceTheGameIsSaved_StillStoresAndPublishesTheEvents()
    {
        using var cancellation = new CancellationTokenSource();
        _gameRepository
            .UpdateAsync(Arg.Any<Game>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cancellation.Cancel();
                return Task.CompletedTask;
            });

        await _service.SkipAsync(GameBuilder.Create(), cancellation.Token);

        await _gameEventRepository.Received(1).AppendAsync(Arg.Any<IReadOnlyCollection<GameEvent>>(), CancellationToken.None);
        await _publisher.Received(1).PublishAsync(Arg.Any<Game>(), Arg.Any<IReadOnlyList<GameEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SkipAsync_OtherPlayersFollow_NpcsPlayUntilItIsTheHumansTurnAgain()
    {
        var game = GameBuilder.Create();

        var events = await _service.SkipAsync(game, CancellationToken.None);

        Assert.True(game.CurrentPlayer.IsHuman);
        Assert.Equal(2, game.RoundNumber);
        Assert.Equal(4, events.Count(gameEvent => gameEvent.Type == GameEventType.TurnSkipped));
    }

    [Fact]
    public async Task SkipAsync_AppendingEventsFailsOnce_RetriesTheSameEvents()
    {
        _gameEventRepository
            .AppendAsync(Arg.Any<IReadOnlyCollection<GameEvent>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new IOException("network")), Task.CompletedTask);
        var game = GameBuilder.Create();

        await _service.SkipAsync(game, CancellationToken.None);

        await _gameEventRepository.Received(2).AppendAsync(Arg.Any<IReadOnlyCollection<GameEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SkipAsync_GameChangedElsewhere_ThrowsAndAppendsNothing()
    {
        _gameRepository
            .UpdateAsync(Arg.Any<Game>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ConcurrencyException("changed")));

        await Assert.ThrowsAsync<ConcurrencyException>(() => _service.SkipAsync(GameBuilder.Create(), CancellationToken.None));

        await _gameEventRepository.DidNotReceive().AppendAsync(Arg.Any<IReadOnlyCollection<GameEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListCampaignsAsync_SearchText_MatchesCountryNamesIgnoringCase()
    {
        var game = GameBuilder.Create();
        _gameRepository.ListByOwnerAsync("user-1", null, Arg.Any<CancellationToken>()).Returns([game]);
        var queries = new GameQueryService(_gameRepository, _gameEventRepository);

        var matching = await queries.ListCampaignsAsync("user-1", "country 2", null, CampaignSort.LastPlayed, CancellationToken.None);
        var notMatching = await queries.ListCampaignsAsync("user-1", "brazil", null, CampaignSort.LastPlayed, CancellationToken.None);

        Assert.Single(matching);
        Assert.Empty(notMatching);
    }
}
