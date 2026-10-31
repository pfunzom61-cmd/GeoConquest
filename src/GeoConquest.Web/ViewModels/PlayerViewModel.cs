namespace GeoConquest.Web.ViewModels;

/// <summary>A participant as shown on the board. The display name is a country or the owner's chosen name, never an account detail.</summary>
public sealed record PlayerViewModel(string Id, string Name, bool IsHuman, bool IsEliminated, string ColorKey, int CountryCount);
