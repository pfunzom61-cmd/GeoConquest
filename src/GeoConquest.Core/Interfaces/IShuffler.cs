namespace GeoConquest.Core.Interfaces;

/// <summary>Source of random ordering for game setup (picking countries, turn order). Abstracted for tests.</summary>
public interface IShuffler
{
    /// <summary>Returns a new list with the same items in random order. The input is not modified.</summary>
    IReadOnlyList<T> Shuffle<T>(IReadOnlyList<T> items);
}
