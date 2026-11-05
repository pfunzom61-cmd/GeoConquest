using GeoConquest.Core.Interfaces;

namespace GeoConquest.Web.BackgroundServices;

/// <summary>
/// Plays the NPC turns of every queued game, several games at a time so one slow game never holds up the others.
/// On startup it first queues every unfinished game that is waiting on an NPC, so play carries on after a restart.
/// </summary>
public sealed class NpcTurnBackgroundService(
    NpcTurnQueue queue,
    INpcTurnProcessor processor,
    IGameRepository gameRepository,
    ILogger<NpcTurnBackgroundService> logger) : BackgroundService
{
    private const int MaxConcurrentGames = 10;
    private const int MaxAttemptsPerGame = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await ResumeUnfinishedGamesAsync(stoppingToken);

            await Parallel.ForEachAsync(
                queue.DequeueAllAsync(stoppingToken),
                new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrentGames, CancellationToken = stoppingToken },
                async (gameId, cancellationToken) => await PlayGameAsync(gameId, cancellationToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The application is shutting down. Every step already played is saved, so nothing is lost.
        }
    }

    private async Task ResumeUnfinishedGamesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var gameIds = await gameRepository.ListGameIdsAwaitingNpcTurnAsync(cancellationToken);
            foreach (var gameId in gameIds)
            {
                queue.Enqueue(gameId);
            }

            logger.LogInformation("Resuming NPC turns in {Count} game(s).", gameIds.Count);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            // Opening or syncing a game queues it again, so a failed scan only delays the games nobody is looking at.
            logger.LogError(exception, "Could not look for games waiting on NPC turns at startup.");
        }
    }

    /// <summary>
    /// Plays one game's NPC turns. A failure is logged and retried a few times; if it persists the game stays saved
    /// where it was and is picked up again the next time it is opened or the application restarts. It never escapes,
    /// because an unhandled exception here would stop every other game too.
    /// </summary>
    private async Task PlayGameAsync(string gameId, CancellationToken cancellationToken)
    {
        try
        {
            for (var attempt = 1; attempt <= MaxAttemptsPerGame; attempt++)
            {
                try
                {
                    await processor.PlayUntilHumanTurnAsync(gameId, cancellationToken);
                    return;
                }
                catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
                {
                    logger.LogError(exception, "Playing NPC turns for game {GameId} failed (attempt {Attempt} of {MaxAttempts}).",
                        gameId, attempt, MaxAttemptsPerGame);

                    if (attempt < MaxAttemptsPerGame)
                    {
                        await Task.Delay(RetryDelay, cancellationToken);
                    }
                }
            }
        }
        finally
        {
            queue.Finished(gameId);
        }
    }
}
