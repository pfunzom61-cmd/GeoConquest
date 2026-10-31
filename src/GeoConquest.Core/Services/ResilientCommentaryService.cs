using GeoConquest.Core.Exceptions;
using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;

namespace GeoConquest.Core.Services;

/// <summary>
/// Wraps the language model so that a round always gets commentary on time. The model is only asked when the budget
/// and the rate limit allow it, its answer must pass the <see cref="CommentaryOutputGuard"/>, and anything else
/// (a timeout, a 429, an error, unusable text) quietly becomes the fallback's line. This method does not fail.
/// </summary>
public sealed class ResilientCommentaryService(
    ICommentaryService model,
    ICommentaryService fallback,
    CommentaryRateLimiter rateLimiter,
    CommentaryOptions options,
    TimeProvider timeProvider) : ICommentaryService
{
    public async Task<GeneratedCommentary> DescribeRoundAsync(BattleContext context, TimeSpan timeBudget, CancellationToken cancellationToken)
    {
        if (timeBudget >= options.MinimumAiBudget && rateLimiter.TryAcquire())
        {
            var answer = await AskModelAsync(context, timeBudget, cancellationToken);
            var cleaned = CommentaryOutputGuard.Clean(answer?.Text, context);
            if (cleaned is not null)
            {
                return answer! with { Text = cleaned };
            }
        }

        return await fallback.DescribeRoundAsync(context, timeBudget, cancellationToken);
    }

    private async Task<GeneratedCommentary?> AskModelAsync(BattleContext context, TimeSpan timeBudget, CancellationToken cancellationToken)
    {
        using var budget = new CancellationTokenSource(timeBudget, timeProvider);
        using var budgetOrCaller = CancellationTokenSource.CreateLinkedTokenSource(budget.Token, cancellationToken);

        try
        {
            return await model.DescribeRoundAsync(context, timeBudget, budgetOrCaller.Token);
        }
        catch (CommentaryProviderException exception)
        {
            if (exception.RetryAfter is { } pause)
            {
                rateLimiter.PauseFor(pause);
            }

            return null;
        }
        catch (Exception)
        {
            // Commentary is decoration: no failure in producing it may ever reach the game.
            return null;
        }
    }
}
