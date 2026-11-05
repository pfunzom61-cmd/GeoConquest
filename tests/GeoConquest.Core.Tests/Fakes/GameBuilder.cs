using GeoConquest.Core.Models;

namespace GeoConquest.Core.Tests.Fakes;

/// <summary>
/// Builds small deterministic games for tests: players P0..Pn-1 (P0 is the human) each owning
/// one country C0..Cn-1, taking turns in id order. Soldier value equals the starting soldiers.
/// </summary>
public static class GameBuilder
{
    public static readonly DateTimeOffset Now = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    public static Game Create(params int[] soldiersPerPlayer)
    {
        if (soldiersPerPlayer.Length == 0)
        {
            soldiersPerPlayer = [5, 5, 5, 5];
        }

        var players = new List<Player>();
        var territories = new List<Territory>();

        for (var index = 0; index < soldiersPerPlayer.Length; index++)
        {
            players.Add(new Player($"P{index}", $"Player {index}", isHuman: index == 0, $"C{index}", $"color{index}"));
            territories.Add(new Territory($"C{index}", $"Country {index}", soldiersPerPlayer[index], $"P{index}", soldiersPerPlayer[index]));
        }

        return new Game("game-1", "user-1", "Bob", players, territories, players.Select(player => player.Id), Now);
    }
}
