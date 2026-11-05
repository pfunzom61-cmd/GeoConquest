namespace GeoConquest.Web.ViewModels;

/// <summary>A country on the picker, with the soldier value it would start with.</summary>
public sealed record CountryOptionViewModel(string IsoCode, string Name, long Population, int SoldierValue);
