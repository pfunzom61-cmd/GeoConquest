using GeoConquest.Core.Enums;
using GeoConquest.Web.Tests.Fakes;

namespace GeoConquest.Web.Tests;

public sealed class PageShellTests(GeoConquestWebFactory factory) : IClassFixture<GeoConquestWebFactory>
{
    [Fact]
    public async Task Navigation_AnonymousVersusSignedIn_ShowsOnlyTheLinksThatApply()
    {
        var anonymous = await (await factory.NewAnonymousClient().GetAsync("/")).Content.ReadAsStringAsync();
        var owner = await factory.RegisterAsync();
        var signedIn = await (await owner.Client.GetAsync("/")).Content.ReadAsStringAsync();

        Assert.Contains("Live games", anonymous);
        Assert.Contains("Log in", anonymous);
        Assert.DoesNotContain("My campaigns", anonymous);
        Assert.Contains("My campaigns", signedIn);
        Assert.Contains("Log out", signedIn);
        Assert.DoesNotContain(">Register<", signedIn);
    }

    [Fact]
    public async Task Board_GameIsOver_HasNoActionPanel()
    {
        var owner = await factory.RegisterAsync();
        var gameId = await GameTestSupport.CreateGameAsync(owner);
        Assert.Contains("data-orders", await (await owner.Client.GetAsync($"/games/{gameId}")).Content.ReadAsStringAsync());

        var game = (await factory.Games.GetByIdAsync(gameId, CancellationToken.None))!;
        game.Finish(GameStatus.Lost, null);
        await factory.Games.UpdateAsync(game, CancellationToken.None);

        var board = await (await owner.Client.GetAsync($"/games/{gameId}")).Content.ReadAsStringAsync();
        Assert.DoesNotContain("data-orders", board);
        Assert.Contains("Start a new campaign", board);
    }
}
