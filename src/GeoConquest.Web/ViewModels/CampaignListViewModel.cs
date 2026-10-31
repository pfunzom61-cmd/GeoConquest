using GeoConquest.Core.Enums;

namespace GeoConquest.Web.ViewModels;

/// <summary>The My Campaigns page: the current search, filter and sort, and the matching games.</summary>
public sealed record CampaignListViewModel(
    string? Search, GameStatus? Status, CampaignSort Sort, IReadOnlyList<CampaignListItemViewModel> Campaigns);
