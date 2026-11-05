using GeoConquest.Core.Interfaces;

namespace GeoConquest.Core.Tests.Fakes;

/// <summary>Deterministic shuffler that reverses the input, so tests can tell whether the shuffler was used.</summary>
public sealed class ReversingShuffler : IShuffler
{
    public IReadOnlyList<T> Shuffle<T>(IReadOnlyList<T> items) => items.Reverse().ToList();
}
