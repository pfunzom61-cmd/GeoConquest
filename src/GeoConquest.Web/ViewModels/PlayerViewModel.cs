namespace GeoConquest.Web.ViewModels;

/// <summary>
/// A participant as shown on the board. The display name is a country or the owner's chosen name, never an account detail.
/// <paramref name="HomeCountryCode"/> is the country the player started with, so the board can tell a conquered country from one still held by its first owner.
/// </summary>
public sealed record PlayerViewModel(
    string Id, string Name, bool IsHuman, bool IsEliminated, string ColorKey, string HomeCountryCode, int CountryCount);
