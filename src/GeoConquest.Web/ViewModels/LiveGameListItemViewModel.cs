using GeoConquest.Core.Models;

namespace GeoConquest.Web.ViewModels;

/// <summary>One game of the public Live Games list. Exposes the owner's display name only, never their user id or email.</summary>
public sealed record LiveGameListItemViewModel(
    string GameId, string OwnerDisplayName, string CountryCode, string CountryName, int Round, string CurrentPlayerName, DateTimeOffset LastActive)
{
    public static LiveGameListItemViewModel From(Game game) => new(
        game.Id,
        game.OwnerDisplayName,
        game.HumanPlayer.HomeCountryCode,
        game.GetTerritory(game.HumanPlayer.HomeCountryCode).CountryName,
        game.RoundNumber,
        game.CurrentPlayer.DisplayName,
        game.UpdatedAt);
}
