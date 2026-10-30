namespace GeoConquest.Web.ViewModels;

/// <summary>What the signed-in player sees about their own account.</summary>
public sealed record ProfileViewModel(string DisplayName, string Email);
