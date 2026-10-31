using System.Collections.Concurrent;
using System.Text.Json;
using GeoConquest.Web.Tests.Fakes;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

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
    public async Task Hub_ClientInvokesAServerMethod_IsRefused()
    {
        var gameId = await GameTestSupport.CreateGameAsync(await factory.RegisterAsync());
        await using var spectator = Connect(gameId);
        await spectator.StartAsync();

        await Assert.ThrowsAsync<HubException>(() => spectator.InvokeAsync("OnConnectedAsync"));
    }

    private HubConnection Connect(string gameId) => new HubConnectionBuilder()
        .WithUrl($"{factory.Server.BaseAddress}hubs/game?gameId={gameId}", options =>
        {
            options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
            options.Transports = HttpTransportType.LongPolling;
        })
        .Build();
}
