using GeoConquest.Core.Exceptions;
using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;

namespace GeoConquest.Core.Services;

/// <inheritdoc />
public sealed class NpcTurnProcessor(
    INpcTurnRunner npcTurnRunner,
    IGameRepository gameRepository,
    IGameProgressRecorder progressRecorder,
    NpcPacingOptions pacing,
    TimeProvider timeProvider) : INpcTurnProcessor
{
    private const int MaxConsecutiveConflicts = 5;

    public async Task PlayUntilHumanTurnAsync(string gameId, CancellationToken cancellationToken)
    {
        var conflicts = 0;

        while (true)
        {
            // Reloading every step picks up changes made meanwhile, such as the owner switching fast-forward on.
            var game = await gameRepository.GetByIdAsync(gameId, cancellationToken);
            if (game is null || !game.AwaitsNpcTurn)
            {
                return;
            }

            try
            {
                var events = npcTurnRunner.PlayStep(game);
                await progressRecorder.RecordProgressAsync(game, events, cancellationToken);
                conflicts = 0;
            }
            catch (ConcurrencyException)
            {
                // The step was played on a stale copy and nothing was saved or published, so throw it away and
                // play it again on a fresh one.
                if (++conflicts >= MaxConsecutiveConflicts)
                {
                    throw;
                }

                continue;
            }

            if (!game.AwaitsNpcTurn)
            {
                return;
            }

            await Task.Delay(game.IsFastForward ? pacing.FastForwardDelay : pacing.NormalDelay, timeProvider, cancellationToken);
        }
    }
}
