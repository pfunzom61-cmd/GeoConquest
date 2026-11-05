using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GeoConquest.Web.Tests.Fakes;

namespace GeoConquest.Web.Tests;

public sealed class GameSyncTests(GeoConquestWebFactory factory) : IClassFixture<GeoConquestWebFactory>
{
    [Theory]
    [InlineData("owner", HttpStatusCode.OK)]
    [InlineData("otherUser", HttpStatusCode.Forbidden)]
    [InlineData("anonymous", HttpStatusCode.Unauthorized)]
    public async Task FastForward_OnlyTheOwnerMayChangeThePace(string caller, HttpStatusCode expected)
    {
        var owner = await factory.RegisterAsync();
        var gameId = await GameTestSupport.CreateGameAsync(owner);
        var requester = caller == "otherUser" ? await factory.RegisterAsync() : owner;
        var client = caller == "anonymous" ? factory.NewAnonymousClient() : requester.Client;

        // The anonymous client has no token page of its own, so it borrows the login form's, like a forged request would.
        var tokenPage = caller == "anonymous" ? "/Account/Login" : "/games/new";
        var response = await client.PostJsonAsync(tokenPage, $"/api/games/{gameId}/fast-forward", new { enabled = true });

        Assert.Equal(expected, response.StatusCode);
        var stored = await factory.Games.GetByIdAsync(gameId, CancellationToken.None);
        Assert.Equal(expected == HttpStatusCode.OK, stored!.IsFastForward);
    }

    [Fact]
    public async Task Sync_GivenTheLastSequenceSeen_ReturnsOnlyTheEventsAfterIt()
    {
        var owner = await factory.RegisterAsync();
        var gameId = await GameTestSupport.CreateGameAsync(owner);
        var everything = await GetSyncAsync(gameId, afterSequence: 0);
        var sequences = everything.GetProperty("newEvents").EnumerateArray().Select(gameEvent => gameEvent.GetProperty("sequence").GetInt32()).ToList();
        Assert.True(sequences.Count > 1);

        var latest = await GetSyncAsync(gameId, afterSequence: sequences[^2]);

        var latestEvent = Assert.Single(latest.GetProperty("newEvents").EnumerateArray());
        Assert.Equal(sequences[^1], latestEvent.GetProperty("sequence").GetInt32());
        Assert.Equal(sequences[^1], latest.GetProperty("state").GetProperty("lastEventSequence").GetInt32());
    }

    [Fact]
    public async Task Sync_AnonymousSpectator_SeesAllTenCountriesWithTheirSoldierCounts()
    {
        var owner = await factory.RegisterAsync();
        var gameId = await GameTestSupport.CreateGameAsync(owner);

        var sync = await GetSyncAsync(gameId, afterSequence: 0);

        var territories = sync.GetProperty("state").GetProperty("territories").EnumerateArray().ToList();
        Assert.Equal(10, territories.Count);
        Assert.All(territories, territory => Assert.True(territory.GetProperty("soldiers").GetInt32() >= 1));
    }

    private async Task<JsonElement> GetSyncAsync(string gameId, int afterSequence)
    {
        var response = await factory.NewAnonymousClient().GetAsync($"/api/games/{gameId}/sync?afterSequence={afterSequence}");
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
