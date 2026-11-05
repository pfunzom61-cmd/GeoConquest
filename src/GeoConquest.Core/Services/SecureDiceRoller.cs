using System.Security.Cryptography;
using GeoConquest.Core.Interfaces;

namespace GeoConquest.Core.Services;

/// <summary>Rolls dice with a cryptographic generator so outcomes cannot be predicted from earlier rolls.</summary>
public sealed class SecureDiceRoller : IDiceRoller
{
    private const int DieFaces = 6;

    public IReadOnlyList<int> Roll(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        var dice = new int[count];
        for (var index = 0; index < count; index++)
        {
            dice[index] = RandomNumberGenerator.GetInt32(1, DieFaces + 1);
        }

        return dice;
    }
}
