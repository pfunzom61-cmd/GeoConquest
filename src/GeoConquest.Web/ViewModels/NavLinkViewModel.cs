namespace GeoConquest.Web.ViewModels;

/// <summary>One link of the main navigation and the page it leads to, so the partial can mark the current page.</summary>
public sealed record NavLinkViewModel(string Label, string Controller, string Action);
