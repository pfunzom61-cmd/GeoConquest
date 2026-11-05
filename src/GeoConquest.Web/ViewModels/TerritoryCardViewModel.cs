namespace GeoConquest.Web.ViewModels;

/// <summary>What the territory card partial needs: the country, who holds it now, and whether the board shows it as conquered.</summary>
public sealed record TerritoryCardViewModel(TerritoryViewModel Territory, PlayerViewModel Owner, int Position, bool ViewerIsOwner)
{
    /// <summary>A country held by anyone other than the player who started with it carries the CONQUERED stamp.</summary>
    public bool IsConquered => Owner.HomeCountryCode != Territory.CountryCode;

    /// <summary>One of three stamp angles, fixed per card so it does not change when the cards are sorted.</summary>
    public int Tilt => Position % 3 + 1;

    public bool IsYours => ViewerIsOwner && Owner.IsHuman;
}
