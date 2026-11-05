namespace GeoConquest.Web.ViewModels;

/// <summary>The attack currently in progress.</summary>
public sealed record BattleViewModel(
    string AttackerCountryCode, string AttackerName, string DefenderCountryCode, string DefenderName, int RoundsFought);
