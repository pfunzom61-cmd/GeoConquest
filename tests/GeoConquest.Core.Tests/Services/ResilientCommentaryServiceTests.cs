using GeoConquest.Core.Enums;
using GeoConquest.Core.Exceptions;
using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;
using GeoConquest.Core.Services;
using GeoConquest.Core.Tests.Fakes;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace GeoConquest.Core.Tests.Services;

public sealed class ResilientCommentaryServiceTests
{
    private const string ModelLine = "Brazil rolls a six and Chile wobbles!";

    private static readonly TimeSpan FullBudget = TimeSpan.FromSeconds(5);

    private readonly ICommentaryService _model = Substitute.For<ICommentaryService>();
    private readonly CommentaryOptions _options = new() { MaxRequestsPerMinute = 2, MinimumAiBudgetMilliseconds = 400 };
    private readonly ManualTimeProvider _clock = new(GameBuilder.Now);
    private readonly ResilientCommentaryService _service;

    public ResilientCommentaryServiceTests()
    {
        _model.DescribeRoundAsync(default!, default, default)
            .ReturnsForAnyArgs(new GeneratedCommentary(ModelLine, CommentarySource.Ai));

        _service = new ResilientCommentaryService(
            _model, new TemplateCommentaryService(), new CommentaryRateLimiter(_options, _clock), _options, _clock);
    }

    private Task<GeneratedCommentary> DescribeAsync(TimeSpan? budget = null) =>
        _service.DescribeRoundAsync(BattleContexts.BrazilAttacksChile, budget ?? FullBudget, CancellationToken.None);

    // A null row means the model answers, but with text the output guard rejects.
    public static TheoryData<Exception?> ModelFailures() =>
    [
        new CommentaryProviderException("Groq could not answer."),
        new HttpRequestException("Connection refused."),
        new OperationCanceledException(),
        new InvalidOperationException("Could not read the answer."),
        null,
    ];

    [Theory]
    [MemberData(nameof(ModelFailures))]
    public async Task DescribeRoundAsync_ModelFailsOrAnswerIsUnusable_FallsBackToTheTemplate(Exception? failure)
    {
        if (failure is null)
        {
            _model.DescribeRoundAsync(default!, default, default)
                .ReturnsForAnyArgs(new GeneratedCommentary("A lovely day out for all.", CommentarySource.Ai));
        }
        else
        {
            _model.DescribeRoundAsync(default!, default, default).ThrowsAsyncForAnyArgs(failure);
        }

        var commentary = await DescribeAsync();

        Assert.Equal(CommentarySource.Template, commentary.Source);
    }

    [Fact]
    public async Task DescribeRoundAsync_RateLimitedWithRetryAfter_StopsAskingUntilThePauseEnds()
    {
        _model.DescribeRoundAsync(default!, default, default)
            .ThrowsAsyncForAnyArgs(new CommentaryProviderException("Groq rate limit reached.", TimeSpan.FromSeconds(30)));

        await DescribeAsync();
        await DescribeAsync();
        await _model.ReceivedWithAnyArgs(1).DescribeRoundAsync(default!, default, default);

        _clock.Advance(TimeSpan.FromSeconds(31));
        await DescribeAsync();
        await _model.ReceivedWithAnyArgs(2).DescribeRoundAsync(default!, default, default);
    }

    [Fact]
    public async Task DescribeRoundAsync_OverThePerMinuteLimit_UsesTheTemplateUntilTheWindowPasses()
    {
        Assert.Equal(CommentarySource.Ai, (await DescribeAsync()).Source);
        Assert.Equal(CommentarySource.Ai, (await DescribeAsync()).Source);
        Assert.Equal(CommentarySource.Template, (await DescribeAsync()).Source);

        _clock.Advance(TimeSpan.FromMinutes(1));
        Assert.Equal(CommentarySource.Ai, (await DescribeAsync()).Source);
    }

    [Fact]
    public async Task DescribeRoundAsync_ModelSlowerThanTheBudget_ReturnsTheTemplateOnceTheBudgetEnds()
    {
        _options.MinimumAiBudgetMilliseconds = 0;
        _model.DescribeRoundAsync(default!, default, default).ReturnsForAnyArgs(async call =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, call.Arg<CancellationToken>());
            return new GeneratedCommentary(ModelLine, CommentarySource.Ai);
        });

        var commentary = await DescribeAsync(TimeSpan.FromMilliseconds(20)).WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(CommentarySource.Template, commentary.Source);
    }

    [Fact]
    public async Task DescribeRoundAsync_BudgetTooShortForTheModelToAnswer_UsesTheTemplateWithoutSpendingARequest()
    {
        var commentary = await DescribeAsync(TimeSpan.Zero);

        Assert.Equal(CommentarySource.Template, commentary.Source);
        await _model.DidNotReceiveWithAnyArgs().DescribeRoundAsync(default!, default, default);
    }
}
