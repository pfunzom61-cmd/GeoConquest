using GeoConquest.Core.Enums;

namespace GeoConquest.Web.ViewModels;

/// <summary>The public Live Games page.</summary>
public sealed record LiveGamesViewModel(
    string? Search, LiveGameStatusFilter Status, LiveGameSort Sort, IReadOnlyList<LiveGameListItemViewModel> Games);
