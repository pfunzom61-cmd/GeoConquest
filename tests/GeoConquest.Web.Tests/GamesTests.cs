using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GeoConquest.Core.Enums;
using GeoConquest.Web.Tests.Fakes;

namespace GeoConquest.Web.Tests;

public sealed class GamesTests(GeoConquestWebFactory factory) : IClassFixture<GeoConquestWebFactory>
{
    [Fact]
    public async Task Create_ChosenCountry_StartsAGameAndShowsItsBoard()
    {
        var owner = await factory.RegisterAsync();

        var created = await owner.Client.SubmitFormAsync("/games/new", new Dictionary<string, string> { ["SelectedCountryCode"] = GameTestSupport.ChosenCountryCode });

        Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);
        var board = await owner.Client.GetAsync(created.Headers.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.OK, board.StatusCode);
        Assert.Contains(owner.DisplayName, await board.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Create_NoCountryChosen_ShowsAValidationMessage()
    {
        var owner = await factory.RegisterAsync();

        var response = await owner.Client.SubmitFormAsync("/games/new", new Dictionary<string, string>());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Choose a country to command.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task MyCampaigns_NotSignedIn_RedirectsToLogin()
    {
        var response = await factory.NewAnonymousClient().GetAsync("/games");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Skip_ByTheOwner_LetsNpcsPlayAndReturnsTheNextHumanTurn()
    {
        var owner = await factory.RegisterAsync();
        var gameId = await GameTestSupport.CreateGameAsync(owner);

        var response = await owner.Client.PostJsonAsync("/games/new", $"/api/games/{gameId}/skip");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var state = result.GetProperty("state");
        Assert.Equal(2, state.GetProperty("round").GetInt32());
        Assert.Equal("human", state.GetProperty("currentPlayerId").GetString());
        Assert.Equal("Reinforce", state.GetProperty("phase").GetString());
        Assert.Contains(result.GetProperty("newEvents").EnumerateArray(), gameEvent => gameEvent.GetProperty("type").GetString() == "TurnSkipped");
        Assert.Contains(result.GetProperty("newEvents").EnumerateArray(), gameEvent => gameEvent.GetProperty("type").GetString() == "NpcDecision");
    }

    [Fact]
    public async Task Skip_ByAnotherUser_IsForbidden()
    {
        var gameId = await GameTestSupport.CreateGameAsync(await factory.RegisterAsync());
        var visitor = await factory.RegisterAsync();

        var response = await visitor.Client.PostJsonAsync("/games/new", $"/api/games/{gameId}/skip");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Skip_NotSignedIn_IsUnauthorized()
    {
        var gameId = await GameTestSupport.CreateGameAsync(await factory.RegisterAsync());

        var response = await factory.NewAnonymousClient().PostJsonAsync("/Account/Login", $"/api/games/{gameId}/skip");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Board_ForASpectator_IsReadOnlyAndHidesAccountDetails()
    {
        var owner = await factory.RegisterAsync();
        var gameId = await GameTestSupport.CreateGameAsync(owner);

        var response = await factory.NewAnonymousClient().GetAsync($"/games/{gameId}");

        var page = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Spectating", page);
        Assert.DoesNotContain("request-verification-token", page);
        Assert.DoesNotContain(owner.Email, page);
    }

    [Theory]
    [InlineData(1, "Choose two different countries")]
    [InlineData(0, "Move at least one soldier")]
    public async Task Transfer_BreakingARule_IsRejectedWithTheReason(int soldiers, string expectedMessage)
    {
        var owner = await factory.RegisterAsync();
        var gameId = await GameTestSupport.CreateGameAsync(owner);
        var ownCountryCode = await FirstOwnCountryCodeAsync(owner, gameId);

        var response = await owner.Client.PostJsonAsync("/games/new", $"/api/games/{gameId}/transfer",
            new { fromCountryCode = ownCountryCode, toCountryCode = ownCountryCode, soldiers });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(expectedMessage, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task LiveGames_GameInProgress_ListsOwnerDisplayNameButNotEmail()
    {
        var owner = await factory.RegisterAsync();
        await GameTestSupport.CreateGameAsync(owner);

        var response = await factory.NewAnonymousClient().GetAsync("/live");

        var page = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(owner.DisplayName, page);
        Assert.DoesNotContain(owner.Email, page);
    }

    [Fact]
    public async Task LiveGames_FinishedGame_IsNotListed()
    {
        var owner = await factory.RegisterAsync();
        var gameId = await GameTestSupport.CreateGameAsync(owner);
        var game = (await factory.Games.GetByIdAsync(gameId, CancellationToken.None))!;
        game.Finish(GameStatus.Lost, winnerPlayerId: null);
        await factory.Games.UpdateAsync(game, CancellationToken.None);

        var response = await factory.NewAnonymousClient().GetAsync("/live");

        Assert.DoesNotContain(owner.DisplayName, await response.Content.ReadAsStringAsync());
    }

    private static async Task<string> FirstOwnCountryCodeAsync(Commander owner, string gameId)
    {
        var state = await (await owner.Client.GetAsync($"/api/games/{gameId}")).Content.ReadFromJsonAsync<JsonElement>();
        return state.GetProperty("territories").EnumerateArray()
            .First(territory => territory.GetProperty("ownerPlayerId").GetString() == "human")
            .GetProperty("countryCode").GetString()!;
    }
}
