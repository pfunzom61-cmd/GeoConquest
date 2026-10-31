using GeoConquest.Core.Models;
using GeoConquest.Web.Tests.Fakes;

namespace GeoConquest.Web.Tests;

public sealed class NpcRunnerStartupTests
{
    [Fact]
    public async Task Startup_GameWaitingOnAnNpcWhenTheAppStopped_ResumesPlayToTheHumansTurn()
    {
        await using var factory = new GeoConquestWebFactory();
        var game = CreateGameWaitingOnAnNpc();
        await factory.Games.AddAsync(game, CancellationToken.None);

        _ = factory.Server; // starting the app is what starts the background runner

        await WaitForHumanTurnAsync(factory, game.Id);
        var resumed = (await factory.Games.GetByIdAsync(game.Id, CancellationToken.None))!;
        Assert.True(resumed.CurrentPlayer.IsHuman);
        Assert.Equal(2, resumed.RoundNumber);
    }

    /// <summary>Three players; the human has already played and two NPCs are still to move in round 1.</summary>
    private static Game CreateGameWaitingOnAnNpc()
    {
        var players = new[]
        {
            new Player("human", "Bob", isHuman: true, "T01", "color-0"),
            new Player("npc-T02", "Two", isHuman: false, "T02", "color-1"),
            new Player("npc-T03", "Three", isHuman: false, "T03", "color-2"),
        };
        var territories = new[]
        {
            new Territory("T01", "One", 3, "human", 3),
            new Territory("T02", "Two", 3, "npc-T02", 3),
            new Territory("T03", "Three", 3, "npc-T03", 3),
        };
        var game = new Game("resume-1", "user-1", "Bob", players, territories, players.Select(player => player.Id), DateTimeOffset.UtcNow);
        game.AdvanceToNextLivingPlayer();
        game.StartTurn(0);
        return game;
    }

    private static async Task WaitForHumanTurnAsync(GeoConquestWebFactory factory, string gameId)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while ((await factory.Games.GetByIdAsync(gameId, timeout.Token))!.AwaitsNpcTurn)
        {
            await Task.Delay(20, timeout.Token);
        }
    }
}
