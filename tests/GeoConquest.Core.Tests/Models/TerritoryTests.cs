using GeoConquest.Core.Exceptions;
using GeoConquest.Core.Models;

namespace GeoConquest.Core.Tests.Models;

public sealed class TerritoryTests
{
    private static Territory CreateTerritory(int soldiers = 3) => new("IN", "India", 9, "P1", soldiers);

    [Fact]
    public void RemoveSoldiers_MoreThanGarrison_Throws()
    {
        var territory = CreateTerritory(soldiers: 3);

        Assert.Throws<GameRuleException>(() => territory.RemoveSoldiers(4));
        Assert.Equal(3, territory.Soldiers);
    }

    [Fact]
    public void RemoveSoldiers_ExactGarrison_LeavesZero()
    {
        var territory = CreateTerritory(soldiers: 3);

        territory.RemoveSoldiers(3);

        Assert.Equal(0, territory.Soldiers);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void RemoveSoldiers_NonPositiveCount_Throws(int count)
    {
        Assert.Throws<GameRuleException>(() => CreateTerritory().RemoveSoldiers(count));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddSoldiers_NonPositiveCount_Throws(int count)
    {
        Assert.Throws<GameRuleException>(() => CreateTerritory().AddSoldiers(count));
    }

    [Fact]
    public void ChangeOwner_ValidGarrison_ReplacesOwnerAndSoldiers()
    {
        var territory = CreateTerritory(soldiers: 1);

        territory.ChangeOwner("P2", 4);

        Assert.Equal("P2", territory.OwnerPlayerId);
        Assert.Equal(4, territory.Soldiers);
    }

    [Fact]
    public void ChangeOwner_EmptyGarrison_Throws()
    {
        Assert.Throws<GameRuleException>(() => CreateTerritory().ChangeOwner("P2", 0));
    }
}
