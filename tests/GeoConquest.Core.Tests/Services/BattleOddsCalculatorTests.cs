using GeoConquest.Core.Services;

namespace GeoConquest.Core.Tests.Services;

public sealed class BattleOddsCalculatorTests
{
    private const double Tolerance = 1e-12;

    private readonly BattleOddsCalculator _calculator = new();

    [Theory]
    [InlineData(2, 1, 1.0 / 2.0)]
    [InlineData(3, 1, 3.0 / 4.0)]
    [InlineData(2, 2, 1.0 / 4.0)]
    [InlineData(3, 2, 1.0 / 2.0)]
    public void AttackerWinProbability_SmallStacks_MatchesHandDerivedValue(int attackers, int defenders, double expected)
    {
        Assert.Equal(expected, _calculator.AttackerWinProbability(attackers, defenders), Tolerance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(7)]
    public void AttackerWinProbability_DefenderHasNoSoldiers_IsCertain(int attackers)
    {
        Assert.Equal(1.0, _calculator.AttackerWinProbability(attackers, 0));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(20)]
    public void AttackerWinProbability_AttackerHasOneSoldier_IsImpossible(int defenders)
    {
        Assert.Equal(0.0, _calculator.AttackerWinProbability(1, defenders));
    }

    [Fact]
    public void AttackerWinProbability_MoreAttackers_NeverLowersTheChance()
    {
        var previous = 0.0;
        for (var attackers = 2; attackers <= 20; attackers++)
        {
            var current = _calculator.AttackerWinProbability(attackers, 6);
            Assert.True(current >= previous, $"{attackers} attackers scored {current}, below {previous}");
            previous = current;
        }
    }

    [Fact]
    public void AttackerWinProbability_MoreDefenders_NeverRaisesTheChance()
    {
        var previous = 1.0;
        for (var defenders = 1; defenders <= 20; defenders++)
        {
            var current = _calculator.AttackerWinProbability(8, defenders);
            Assert.True(current <= previous, $"{defenders} defenders scored {current}, above {previous}");
            previous = current;
        }
    }

    [Fact]
    public void AttackerWinProbability_OverwhelmingStack_IsNearlyCertain()
    {
        Assert.InRange(_calculator.AttackerWinProbability(10, 1), 0.95, 1.0);
    }

    [Fact]
    public void AttackerWinProbability_LargeEvenStacks_StaysAProbability()
    {
        var probability = _calculator.AttackerWinProbability(60, 60);

        Assert.InRange(probability, 0.0, 1.0);
    }
}
