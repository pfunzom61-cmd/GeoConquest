using GeoConquest.Core.Interfaces;

namespace GeoConquest.Core.Tests.Fakes;

/// <summary>Plays out a whole game from one seed, so a simulated game is repeatable.</summary>
public sealed class SeededDiceRoller(int seed) : IDiceRoller
{
    private readonly Random _random = new(seed);

    public IReadOnlyList<int> Roll(int count) => Enumerable.Range(0, count).Select(_ => _random.Next(1, 7)).ToList();
}
