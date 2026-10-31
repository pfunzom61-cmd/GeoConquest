using GeoConquest.Core.Models;

namespace GeoConquest.Web.ViewModels;

/// <summary>One row of My Campaigns. A campaign is named after the country the owner commands.</summary>
public sealed record CampaignListItemViewModel(
    string GameId, string CountryName, string Status, int Round, int CountriesOwned, DateTimeOffset LastPlayed)
{
    public static CampaignListItemViewModel From(Game game)
    {
        var human = game.HumanPlayer;
        return new CampaignListItemViewModel(
            game.Id,
            game.GetTerritory(human.HomeCountryCode).CountryName,
            game.Status.ToString(),
            game.RoundNumber,
            game.TerritoriesOwnedBy(human.Id).Count,
            game.UpdatedAt);
    }
}
