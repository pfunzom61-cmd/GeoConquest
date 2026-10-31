using GeoConquest.Core.Enums;
using GeoConquest.Core.Services;
using GeoConquest.Core.Tests.Fakes;

namespace GeoConquest.Core.Tests.Services;

public sealed class TemplateCommentaryServiceTests
{
    private readonly TemplateCommentaryService _service = new();

    [Theory]
    [InlineData(BattleOutcome.Continuing)]
    [InlineData(BattleOutcome.Conquered)]
    [InlineData(BattleOutcome.Failed)]
    public async Task DescribeRoundAsync_AnyOutcome_NamesBothCountriesAndTheDice(BattleOutcome outcome)
    {
        var context = BattleContexts.BrazilAttacksChile with { Outcome = outcome };

        var commentary = await _service.DescribeRoundAsync(context, TimeSpan.Zero, CancellationToken.None);

        Assert.Equal(CommentarySource.Template, commentary.Source);
        Assert.Contains("Brazil", commentary.Text);
        Assert.Contains("Chile", commentary.Text);
        Assert.Contains("6, 2", commentary.Text);
        Assert.Contains("5, 3", commentary.Text);
    }

    [Fact]
    public async Task DescribeRoundAsync_SameFacts_ReadTheSameWhileTheNextRoundReadsDifferently()
    {
        var round = BattleContexts.BrazilAttacksChile;
        var nextRound = round with { BattleRoundNumber = round.BattleRoundNumber + 1 };

        var first = await _service.DescribeRoundAsync(round, TimeSpan.Zero, CancellationToken.None);
        var again = await _service.DescribeRoundAsync(round, TimeSpan.Zero, CancellationToken.None);
        var next = await _service.DescribeRoundAsync(nextRound, TimeSpan.Zero, CancellationToken.None);

        Assert.Equal(first, again);
        Assert.NotEqual(first.Text, next.Text);
    }
}
