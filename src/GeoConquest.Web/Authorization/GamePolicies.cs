namespace GeoConquest.Web.Authorization;

/// <summary>Names of the authorization policies used for games.</summary>
public static class GamePolicies
{
    /// <summary>Only the signed-in owner of a game may play it. Watching is open to everyone.</summary>
    public const string CanPlayGame = "CanPlayGame";
}
