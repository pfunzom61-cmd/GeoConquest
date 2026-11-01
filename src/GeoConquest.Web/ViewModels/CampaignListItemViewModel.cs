using GeoConquest.Core.Enums;
using GeoConquest.Core.Models;

namespace GeoConquest.Web.ViewModels;

/// <summary>One campaign of My Campaigns. A campaign is named after the country the owner commands.</summary>
public sealed record CampaignListItemViewModel(
    string GameId, string CountryCode, string CountryName, string Status, int Round, int CountriesOwned, DateTimeOffset LastPlayed)
{
    public bool IsInProgress => Status == nameof(GameStatus.InProgress);

    /// <summary>The status in words a player would use.</summary>
    public string StatusLabel => Status switch
    {
        nameof(GameStatus.InProgress) => "In progress",
        nameof(GameStatus.Won) => "Won",
        _ => "Lost",
    };

    public static CampaignListItemViewModel From(Game game)
    {
        var human = game.HumanPlayer;
        return new CampaignListItemViewModel(
            game.Id,
            human.HomeCountryCode,
            game.GetTerritory(human.HomeCountryCode).CountryName,
            game.Status.ToString(),
            game.RoundNumber,
            game.TerritoriesOwnedBy(human.Id).Count,
            game.UpdatedAt);
    }
}
