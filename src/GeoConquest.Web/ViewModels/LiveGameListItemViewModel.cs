using GeoConquest.Core.Enums;
using GeoConquest.Core.Models;

namespace GeoConquest.Web.ViewModels;

/// <summary>
/// One game of the public Live Games list. Exposes the owner's display name (the name they chose when registering)
/// only, never their user id or email.
/// </summary>
public sealed record LiveGameListItemViewModel(
    string GameId, string OwnerDisplayName, string CountryCode, string CountryName, GameStatus Status, int Round, string CurrentPlayerName, DateTimeOffset LastActive)
{
    public bool IsInProgress => Status == GameStatus.InProgress;

    public static LiveGameListItemViewModel From(Game game) => new(
        game.Id,
        game.OwnerDisplayName,
        game.HumanPlayer.HomeCountryCode,
        game.GetTerritory(game.HumanPlayer.HomeCountryCode).CountryName,
        game.Status,
        game.RoundNumber,
        game.CurrentPlayer.DisplayName,
        game.UpdatedAt);
}
