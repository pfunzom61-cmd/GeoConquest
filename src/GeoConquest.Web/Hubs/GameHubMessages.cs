namespace GeoConquest.Web.Hubs;

/// <summary>Names of the messages the server sends to the board script (board-live.js listens for the same strings).</summary>
public static class GameHubMessages
{
    /// <summary>New events, oldest first.</summary>
    public const string EventsAppended = "EventsAppended";

    /// <summary>The game as a spectator sees it, after the events above were applied.</summary>
    public const string StateUpdated = "StateUpdated";
}
