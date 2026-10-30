namespace GeoConquest.Core.Models;

/// <summary>One participant in a game: the human, or an NPC representing a country.</summary>
public sealed class Player
{
    public Player(string id, string displayName, bool isHuman, string homeCountryCode, string colorKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(homeCountryCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(colorKey);

        Id = id;
        DisplayName = displayName;
        IsHuman = isHuman;
        HomeCountryCode = homeCountryCode;
        ColorKey = colorKey;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public bool IsHuman { get; }

    public string HomeCountryCode { get; }

    /// <summary>Identifies the player's colour on the board (the UI also adds a non-colour marker).</summary>
    public string ColorKey { get; }

    public bool IsEliminated { get; private set; }

    /// <summary>Marks the player as out of the game after losing their last country.</summary>
    public void Eliminate() => IsEliminated = true;
}
