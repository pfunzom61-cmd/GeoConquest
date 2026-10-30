using GeoConquest.Core.Interfaces;

namespace GeoConquest.Core.Tests.Fakes;

/// <summary>
/// Returns scripted dice. Each call to <see cref="Roll"/> consumes the next script entry, which must have the
/// requested length, so a test also verifies how many dice the code under test asked for.
/// </summary>
public sealed class FixedDiceRoller(params int[][] rolls) : IDiceRoller
{
    private readonly Queue<int[]> _rolls = new(rolls);

    public IReadOnlyList<int> Roll(int count)
    {
        var next = _rolls.Dequeue();
        if (next.Length != count)
        {
            throw new InvalidOperationException($"Test script has {next.Length} dice but {count} were requested.");
        }

        return next;
    }
}
