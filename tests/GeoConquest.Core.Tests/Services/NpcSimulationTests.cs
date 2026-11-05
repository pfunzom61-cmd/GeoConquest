using GeoConquest.Core.Enums;
using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;
using GeoConquest.Core.Services;
using GeoConquest.Core.Tests.Fakes;
using NSubstitute;

namespace GeoConquest.Core.Tests.Services;

public sealed class NpcSimulationTests
{
    private const int MaxRounds = 300;

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void PlayStep_TenNpcsWithSeededDice_FinishesTheGameWithEveryDecisionJustified(int seed)
    {
        var engine = new GameEngine(
            Substitute.For<IGameSetupService>(),
            new BattleService(new SeededDiceRoller(seed)),
            new ReinforcementService(),
            new FixedTimeProvider(GameBuilder.Now));
        var executor = new NpcTurnExecutor(engine, new StrategicNpcStrategy(new BattleOddsCalculator()));
        var game = CreateTenNpcGame();
        var events = new List<GameEvent>();

        while (game.Status == GameStatus.InProgress && game.RoundNumber <= MaxRounds)
        {
            events.AddRange(executor.PlayStep(game));
            Assert.All(game.Territories, territory => Assert.True(territory.Soldiers >= 1));
        }

        Assert.NotEqual(GameStatus.InProgress, game.Status);
        Assert.NotNull(game.WinnerPlayerId);
        var decisions = events.Where(@event => @event.Type == GameEventType.NpcDecision).ToList();
        Assert.NotEmpty(decisions);
        Assert.All(decisions, decision => Assert.Matches(@"\d", decision.Message));
    }

    /// <summary>Ten NPCs, no human, each starting with one country worth 1 to 9 soldiers.</summary>
    private static Game CreateTenNpcGame()
    {
        int[] soldierValues = [1, 1, 2, 3, 3, 4, 5, 6, 8, 9];
        var players = Enumerable.Range(0, soldierValues.Length)
            .Select(index => new Player($"N{index}", $"Npc {index}", isHuman: false, $"C{index}", $"color{index}")).ToList();
        var territories = soldierValues
            .Select((value, index) => new Territory($"C{index}", $"Country {index}", value, $"N{index}", value)).ToList();

        return new Game("simulation", "user-1", "Bob", players, territories, players.Select(player => player.Id), GameBuilder.Now);
    }
}
