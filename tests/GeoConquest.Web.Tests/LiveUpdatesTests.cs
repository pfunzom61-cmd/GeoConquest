using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using GeoConquest.Web.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace GeoConquest.Web.Tests;

public sealed class LiveUpdatesTests(GeoConquestWebFactory factory) : IClassFixture<GeoConquestWebFactory>
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Hub_AnonymousSpectator_ReceivesTheOwnersMovesAndNpcJustificationsWithoutAccountDetails()
    {
        var owner = await factory.RegisterAsync();
        var gameId = await GameTestSupport.CreateGameAsync(owner);
        var game = (await factory.Games.GetByIdAsync(gameId, CancellationToken.None))!;
        var ownerUserId = game.OwnerUserId;
        var eventMessages = new ConcurrentQueue<string>();
        var stateReceived = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var spectator = Connect(gameId);
        spectator.On<JsonElement>("EventsAppended", events => eventMessages.Enqueue(events.GetRawText()));
        spectator.On<JsonElement>("StateUpdated", state =>
        {
            // The owner's own move arrives first; the NPCs' turns, played in the background, follow.
            if (string.Concat(eventMessages).Contains("NpcDecision"))
            {
                stateReceived.TrySetResult(state.GetRawText());
            }
        });
        await spectator.StartAsync();

        await owner.Client.PostJsonAsync("/games/new", $"/api/games/{gameId}/skip");

        var state = await stateReceived.Task.WaitAsync(Patience);
        var everythingReceived = string.Concat(eventMessages) + state;
        Assert.Contains("TurnSkipped", everythingReceived);
        Assert.Contains("NpcDecision", everythingReceived);
        Assert.Contains("\"viewerIsOwner\":false", state);
        Assert.DoesNotContain(owner.Email, everythingReceived);
        Assert.DoesNotContain(ownerUserId, everythingReceived);
        Assert.NotEqual(ownerUserId, game.HumanPlayer.Id); // viewers see the in-game player id, never the account id
    }

    [Fact]
    public async Task Roll_WithNoApiKey_ReturnsFirstThenTemplateCommentaryReachesSpectatorsAndSurvivesAReload()
    {
        var owner = await factory.RegisterAsync();
        var gameId = await GameTestSupport.CreateGameAsync(owner);
        var game = (await factory.Games.GetByIdAsync(gameId, CancellationToken.None))!;
        var attacker = game.GetTerritory(game.HumanPlayer.HomeCountryCode);
        attacker.AddSoldiers(5);
        await factory.Games.UpdateAsync(game, CancellationToken.None);
        var defender = game.Territories.First(territory => territory.OwnerPlayerId != game.HumanPlayer.Id);

        var pushedCommentary = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var spectator = Connect(gameId);
        spectator.On<JsonElement>("EventsAppended", events =>
        {
            foreach (var gameEvent in events.EnumerateArray().Where(IsCommentary))
            {
                pushedCommentary.TrySetResult(gameEvent.Clone());
            }
        });
        await spectator.StartAsync();

        await owner.Client.PostJsonAsync("/games/new", $"/api/games/{gameId}/attack",
            new { attackerCountryCode = attacker.CountryCode, defenderCountryCode = defender.CountryCode });
        var rollResponse = await owner.Client.PostJsonAsync("/games/new", $"/api/games/{gameId}/roll");
        var roll = await rollResponse.Content.ReadFromJsonAsync<JsonElement>();

        var newEvents = roll.GetProperty("newEvents").EnumerateArray().ToList();
        Assert.DoesNotContain(newEvents, IsCommentary);
        var dice = newEvents.Single(gameEvent => gameEvent.GetProperty("type").GetString() == "DiceRolled");

        var commentary = await pushedCommentary.Task.WaitAsync(Patience);
        Assert.Equal(dice.GetProperty("sequence").GetInt32() + 1, commentary.GetProperty("sequence").GetInt32());
        Assert.Equal("Template", commentary.GetProperty("commentarySource").GetString());

        var reloadResponse = await factory.NewAnonymousClient().GetAsync($"/api/games/{gameId}/sync?afterSequence=0");
        var reload = await reloadResponse.Content.ReadFromJsonAsync<JsonElement>();
        var stored = reload.GetProperty("newEvents").EnumerateArray().Single(IsCommentary);
        Assert.Equal(commentary.GetProperty("message").GetString(), stored.GetProperty("message").GetString());
        Assert.Equal(roll.GetProperty("state").GetProperty("version").GetInt32(), reload.GetProperty("state").GetProperty("version").GetInt32());
    }

    [Fact]
    public void BoardScripts_AnyScript_NeverInsertsTextAsHtml()
    {
        var webRoot = factory.Services.GetRequiredService<IWebHostEnvironment>().WebRootPath;
        var scripts = Directory.GetFiles(Path.Combine(webRoot, "js"), "*.js");

        Assert.Contains(scripts, script => Path.GetFileName(script) == "board.js");
        Assert.All(scripts, script =>
        {
            var source = File.ReadAllText(script);
            Assert.DoesNotContain("innerHTML", source);
            Assert.DoesNotContain("outerHTML", source);
            Assert.DoesNotContain("insertAdjacentHTML", source);
            Assert.DoesNotContain("document.write", source);
        });
    }

    [Fact]
    public async Task Hub_ClientInvokesAServerMethod_IsRefused()
    {
        var gameId = await GameTestSupport.CreateGameAsync(await factory.RegisterAsync());
        await using var spectator = Connect(gameId);
        await spectator.StartAsync();

        await Assert.ThrowsAsync<HubException>(() => spectator.InvokeAsync("OnConnectedAsync"));
    }

    private static bool IsCommentary(JsonElement gameEvent) => gameEvent.GetProperty("type").GetString() == "Commentary";

    private HubConnection Connect(string gameId) => new HubConnectionBuilder()
        .WithUrl($"{factory.Server.BaseAddress}hubs/game?gameId={gameId}", options =>
        {
            options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
            options.Transports = HttpTransportType.LongPolling;
        })
        .Build();
}
