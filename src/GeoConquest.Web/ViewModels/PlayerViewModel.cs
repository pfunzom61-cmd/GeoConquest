namespace GeoConquest.Web.ViewModels;

/// <summary>
/// A participant as shown on the board. The display name is a country or the owner's chosen name, never an account detail.
/// <paramref name="HomeCountryCode"/> is the country the player started with, so the board can tell a conquered country from one still held by its first owner.
/// </summary>
public sealed record PlayerViewModel(
    string Id, string Name, bool IsHuman, bool IsEliminated, string ColorKey, string HomeCountryCode, int CountryCount)
{
    /// <summary>Two characters that mark the player next to their colour and pattern: the country code for an NPC, the start of the name for the human.</summary>
    public string Initials => IsHuman ? InitialsOf(Name) : HomeCountryCode;

    /// <summary>The first two letters or digits of a name, in capitals.</summary>
    public static string InitialsOf(string name) => new string(name.Where(char.IsLetterOrDigit).Take(2).ToArray()).ToUpperInvariant();

    /// <summary>How the board names this player in words, so that no one is told apart by colour alone.</summary>
    public string Label(bool viewerIsOwner) => !IsHuman ? $"{Name} (NPC)" : viewerIsOwner ? $"{Name} (you)" : $"{Name} (player)";
}
