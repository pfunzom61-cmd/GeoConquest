namespace GeoConquest.Web.ViewModels;

/// <summary>
/// The plain-words labels the board page prints before its script runs. board-text.js holds the same wording for the
/// updates that arrive afterwards, so a change here must be made there too.
/// </summary>
public static class BoardText
{
    public static string PhaseLabel(string phase) => phase switch
    {
        "Reinforce" => "Placing reinforcements",
        "Action" => "Choosing an action",
        "Attacking" => "Attack in progress",
        "PostAttack" => "After the attack",
        _ => "Game over",
    };

    public static string TurnTitle(GameStateViewModel state)
    {
        if (state.Status != "InProgress")
        {
            return "Game over";
        }

        var current = state.Players.First(player => player.Id == state.CurrentPlayerId);
        return current.IsHuman && state.ViewerIsOwner ? "Your turn" : $"{current.Label(state.ViewerIsOwner)} is playing";
    }

    public static string CountryCount(int count) => count == 1 ? "1 country" : $"{count} countries";

    public static string ResultTitle(string status) => status switch
    {
        "Won" => "Victory",
        "Lost" => "Defeat",
        _ => "Game over",
    };
}
