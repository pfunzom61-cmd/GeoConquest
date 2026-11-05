using System.Security.Cryptography;
using GeoConquest.Core.Interfaces;

namespace GeoConquest.Core.Services;

/// <summary>Fisher-Yates shuffle driven by a cryptographic generator.</summary>
public sealed class SecureShuffler : IShuffler
{
    public IReadOnlyList<T> Shuffle<T>(IReadOnlyList<T> items)
    {
        var shuffled = items.ToArray();

        for (var index = shuffled.Length - 1; index > 0; index--)
        {
            var swapIndex = RandomNumberGenerator.GetInt32(index + 1);
            (shuffled[index], shuffled[swapIndex]) = (shuffled[swapIndex], shuffled[index]);
        }

        return shuffled;
    }
}
