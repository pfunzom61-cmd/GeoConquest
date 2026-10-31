using Microsoft.AspNetCore.Mvc.Testing;

namespace GeoConquest.Web.Tests.Fakes;

/// <summary>A registered, signed-in user of the app under test.</summary>
public sealed record Commander(FormPostingClient Client, string Email, string DisplayName);

/// <summary>Steps shared by the tests that need a signed-in owner with a game.</summary>
public static class GameTestSupport
{
    /// <summary>A country in the test catalogue that the owner commands.</summary>
    public const string ChosenCountryCode = "T01";

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

    /// <summary>Starts a game for the owner and returns its id.</summary>
    public static async Task<string> CreateGameAsync(Commander owner)
    {
        var created = await owner.Client.SubmitFormAsync("/games/new", new Dictionary<string, string> { ["SelectedCountryCode"] = ChosenCountryCode });
        return created.Headers.Location!.OriginalString.Split('/').Last();
    }
}
