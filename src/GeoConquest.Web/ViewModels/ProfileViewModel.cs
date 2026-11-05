using GeoConquest.Core.Enums;
using GeoConquest.Core.Models;

namespace GeoConquest.Web.ViewModels;

/// <summary>
/// The signed-in player's own file. It is only ever built for the account that is signed in, and shows their email to
/// them alone: the public pages carry display names only.
/// </summary>
public sealed record ProfileViewModel(
    string DisplayName, string Email, string Initials, int CampaignsPlayed, int CampaignsWon, int CountriesConquered)
{
    /// <summary>
    /// "Countries conquered" counts the countries the player holds across their campaigns that they did not start
    /// with, so a country lost again is not counted.
    /// </summary>
    public static ProfileViewModel From(string displayName, string email, IReadOnlyList<Game> campaigns) => new(
        displayName,
        email,
        PlayerViewModel.InitialsOf(displayName),
        campaigns.Count,
        campaigns.Count(game => game.Status == GameStatus.Won),
        campaigns.Sum(game => game.TerritoriesOwnedBy(game.HumanPlayer.Id).Count(territory => territory.CountryCode != game.HumanPlayer.HomeCountryCode)));
}
