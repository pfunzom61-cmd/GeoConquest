using GeoConquest.Core.Models;

namespace GeoConquest.Core.Interfaces;

/// <summary>Writes one or two lively, family-friendly sentences about a dice round, from its facts alone.</summary>
public interface ICommentaryService
{
    /// <param name="context">The facts of the round.</param>
    /// <param name="timeBudget">How long the caller is prepared to wait for this line.</param>
    /// <param name="cancellationToken">Cancels the work.</param>
    Task<GeneratedCommentary> DescribeRoundAsync(BattleContext context, TimeSpan timeBudget, CancellationToken cancellationToken);
}
