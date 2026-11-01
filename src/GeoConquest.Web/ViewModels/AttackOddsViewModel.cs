namespace GeoConquest.Web.ViewModels;

/// <summary>The estimated chance (0 to 1) that one of the owner's countries conquers one target. Sent to the owner only.</summary>
public sealed record AttackOddsViewModel(string AttackerCountryCode, string DefenderCountryCode, double WinChance);
