namespace GeoConquest.Web.ViewModels;

/// <summary>The public Live Games page.</summary>
public sealed record LiveGamesViewModel(string? Search, IReadOnlyList<LiveGameListItemViewModel> Games);
