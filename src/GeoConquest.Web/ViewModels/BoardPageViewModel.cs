namespace GeoConquest.Web.ViewModels;

/// <summary>The game page: the current state plus the latest entries of the event log.</summary>
public sealed record BoardPageViewModel(GameStateViewModel State, IReadOnlyList<GameEventViewModel> RecentEvents);
