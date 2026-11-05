using GeoConquest.Core.Services;

namespace GeoConquest.Core.Tests.Services;

public sealed class SoldierAllocatorTests
{
    private readonly SoldierAllocator _allocator = new();

    [Theory]
    [InlineData(5_000_000, 1)]
    [InlineData(5_000_001, 2)]
    [InlineData(10_000_000, 2)]
    [InlineData(10_000_001, 3)]
    [InlineData(25_000_000, 3)]
    [InlineData(25_000_001, 4)]
    [InlineData(50_000_000, 4)]
    [InlineData(50_000_001, 5)]
    [InlineData(75_000_000, 5)]
    [InlineData(75_000_001, 6)]
    [InlineData(100_000_000, 6)]
    [InlineData(100_000_001, 7)]
    [InlineData(125_000_000, 7)]
    [InlineData(125_000_001, 8)]
    [InlineData(150_000_000, 8)]
    [InlineData(150_000_001, 9)]
    public void GetSoldierValue_AtTierBoundary_UsesInclusiveUpperBound(long population, int expectedSoldiers)
    {
        Assert.Equal(expectedSoldiers, _allocator.GetSoldierValue(population));
    }

    [Theory]
    [InlineData(1_450_000_000, 9)]
    [InlineData(800, 1)]
    [InlineData(0, 1)]
    public void GetSoldierValue_ExtremePopulations_StayWithinTiers(long population, int expectedSoldiers)
    {
        Assert.Equal(expectedSoldiers, _allocator.GetSoldierValue(population));
    }

    [Fact]
    public void GetSoldierValue_NegativePopulation_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _allocator.GetSoldierValue(-1));
    }
}
