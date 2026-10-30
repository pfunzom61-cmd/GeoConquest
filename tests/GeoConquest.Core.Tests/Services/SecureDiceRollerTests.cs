using GeoConquest.Core.Services;

namespace GeoConquest.Core.Tests.Services;

public sealed class SecureDiceRollerTests
{
    [Fact]
    public void Roll_RequestedCount_ReturnsThatManyDiceBetweenOneAndSix()
    {
        var dice = new SecureDiceRoller().Roll(500);

        Assert.Equal(500, dice.Count);
        Assert.All(dice, die => Assert.InRange(die, 1, 6));
        Assert.Equal(6, dice.Distinct().Count());
    }

    [Fact]
    public void Roll_Zero_ReturnsNoDice()
    {
        Assert.Empty(new SecureDiceRoller().Roll(0));
    }
}
