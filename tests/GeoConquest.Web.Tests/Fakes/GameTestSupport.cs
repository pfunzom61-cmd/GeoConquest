using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GeoConquest.Web.Tests.Fakes;

/// <summary>A registered, signed-in user of the app under test.</summary>
public sealed record Commander(FormPostingClient Client, string Email, string DisplayName);

/// <summary>Steps shared by the tests that need a signed-in owner with a game.</summary>
public static class GameTestSupport
{
    /// <summary>A country in the test catalogue that the owner commands.</summary>
    public const string ChosenCountryCode = "T01";

    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

    public static FormPostingClient NewAnonymousClient(this GeoConquestWebFactory factory) =>
        new(factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }));

    public static async Task<Commander> RegisterAsync(this GeoConquestWebFactory factory)
    {
        var client = factory.NewAnonymousClient();
        var email = $"{Guid.NewGuid():N}@example.org";
        var displayName = $"Cmdr{Guid.NewGuid():N}"[..14];

        await client.SubmitFormAsync("/Account/Register", new Dictionary<string, string>
        {
            ["DisplayName"] = displayName,
            ["Email"] = email,
            ["Password"] = "Conquest1",
            ["ConfirmPassword"] = "Conquest1",
        });

        return new Commander(client, email, displayName);
    }

    /// <summary>Starts a game for the owner, waits for the NPCs ahead of them to finish, and returns its id.</summary>
    public static async Task<string> CreateGameAsync(Commander owner)
    {
        var created = await owner.Client.SubmitFormAsync("/games/new", new Dictionary<string, string> { ["SelectedCountryCode"] = ChosenCountryCode });
        var gameId = created.Headers.Location!.OriginalString.Split('/').Last();

        await WaitForStateAsync(owner, gameId, state => state.GetProperty("currentPlayerId").GetString() == "human");
        return gameId;
    }

    /// <summary>Polls the game's state until <paramref name="isReached"/> is true, because NPC turns now play in the background.</summary>
    public static async Task<JsonElement> WaitForStateAsync(Commander viewer, string gameId, Func<JsonElement, bool> isReached)
    {
        using var timeout = new CancellationTokenSource(Patience);
        while (true)
        {
            var state = await (await viewer.Client.GetAsync($"/api/games/{gameId}")).Content.ReadFromJsonAsync<JsonElement>(timeout.Token);
            if (isReached(state))
            {
                return state;
            }

            await Task.Delay(20, timeout.Token);
        }
    }
}
