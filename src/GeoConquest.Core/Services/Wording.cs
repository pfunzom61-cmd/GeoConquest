using GeoConquest.Core.Models;

namespace GeoConquest.Core.Services;

/// <summary>Builds the pieces of event text that must read correctly for every count and every kind of player.</summary>
public static class Wording
{
    /// <summary>"1 soldier", "2 soldiers": the noun gets its plural unless the count is exactly one.</summary>
    public static string Count(int count, string noun) => count == 1 ? $"1 {noun}" : $"{count} {noun}s";

    /// <summary>
    /// A player's name as the log shows it. An NPC carries the name of its home country, so it is marked "(NPC)" to keep
    /// it apart from the country of the same name, which another player may hold.
    /// </summary>
    public static string PlayerName(Player player) => player.IsHuman ? player.DisplayName : $"{player.DisplayName} (NPC)";

    /// <summary>A country together with whoever holds it: "Madagascar, held by SamSlayer".</summary>
    public static string HeldCountry(Territory territory, Player holder) =>
        $"{territory.CountryName}, held by {PlayerName(holder)}";
}
