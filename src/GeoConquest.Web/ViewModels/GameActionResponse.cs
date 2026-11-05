namespace GeoConquest.Web.ViewModels;

/// <summary>Returned by every action: the game as it now stands and the events the action (and the NPC turns after it) produced.</summary>
public sealed record GameActionResponse(GameStateViewModel State, IReadOnlyList<GameEventViewModel> NewEvents);
