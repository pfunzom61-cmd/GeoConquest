using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;

namespace GeoConquest.Web.BackgroundServices;

/// <summary>
/// Writes the queued commentary, away from the moves that caused it, so neither a human action nor an NPC step ever
/// waits for the language model. Several run at once so that a slow answer does not hold up the next round's line.
/// </summary>
public sealed class CommentaryBackgroundService(
    CommentaryQueue queue,
    ICommentaryProcessor processor,
    ILogger<CommentaryBackgroundService> logger) : BackgroundService
{
    private const int MaxConcurrentRequests = 8;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Parallel.ForEachAsync(
                queue.DequeueAllAsync(stoppingToken),
                new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrentRequests, CancellationToken = stoppingToken },
                async (request, cancellationToken) => await WriteCommentaryAsync(request, cancellationToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The application is shutting down. A round that misses its line loses nothing else.
        }
    }

    /// <summary>A failure is logged and goes no further: it costs one round its line, and must not stop the others.</summary>
    private async Task WriteCommentaryAsync(CommentaryRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await processor.ProcessAsync(request, cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(exception, "Could not store commentary {Sequence} for game {GameId}.", request.Sequence, request.GameId);
        }
    }
}
