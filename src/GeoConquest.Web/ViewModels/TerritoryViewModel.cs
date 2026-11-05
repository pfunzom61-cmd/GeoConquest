namespace GeoConquest.Web.ViewModels;

public sealed record TerritoryViewModel(
    string CountryCode, string CountryName, string OwnerPlayerId, string OwnerName, int Soldiers, int SoldierValue);
