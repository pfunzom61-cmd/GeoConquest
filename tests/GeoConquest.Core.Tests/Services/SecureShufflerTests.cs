using GeoConquest.Core.Services;

namespace GeoConquest.Core.Tests.Services;

public sealed class SecureShufflerTests
{
    [Fact]
    public void Shuffle_AnyList_KeepsSameItemsAndLeavesInputUntouched()
    {
        int[] original = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

        var shuffled = new SecureShuffler().Shuffle(original);

        Assert.Equal(original.Order(), shuffled.Order());
        Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8, 9, 10], original);
    }
}
