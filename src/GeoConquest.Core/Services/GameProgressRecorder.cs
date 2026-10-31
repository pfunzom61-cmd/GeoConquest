using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;

namespace GeoConquest.Core.Services;

/// <inheritdoc />
public sealed class GameProgressRecorder(
    IGameRepository gameRepository,
    IGameEventRepository gameEventRepository,
    IGameUpdatePublisher publisher,
    BattleCommentaryScheduler commentaryScheduler) : IGameProgressRecorder
{
    private const int MaxAppendAttempts = 3;

    public async Task RecordNewGameAsync(Game game, IReadOnlyCollection<GameEvent> events, CancellationToken cancellationToken)
    {
        await gameRepository.AddAsync(game, cancellationToken);
        await AppendEventsAsync(events);
    }

    public async Task RecordProgressAsync(Game game, IReadOnlyList<GameEvent> events, CancellationToken cancellationToken)
    {
        await gameRepository.UpdateAsync(game, cancellationToken);

        // From here the game is saved, so the rest must not be abandoned halfway. Viewers are told even if the log
        // could not be written, because what they would see is true.
        try
        {
            await AppendEventsAsync(events);
        }
        finally
        {
            await publisher.PublishAsync(game, events, CancellationToken.None);
            commentaryScheduler.ScheduleFor(game, events);
        }
    }

    public async Task RecordCommentaryAsync(GameEvent commentary)
    {
        try
        {
            await AppendEventsAsync([commentary]);
        }
        finally
        {
            await publisher.PublishEventsAsync(commentary.GameId, [commentary], CancellationToken.None);
        }
    }

    private async Task AppendEventsAsync(IReadOnlyCollection<GameEvent> events)
    {
        if (events.Count == 0)
        {
            return;
        }

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await gameEventRepository.AppendAsync(events, CancellationToken.None);
                return;
            }
            catch (Exception) when (attempt < MaxAppendAttempts)
            {
                // Appending is idempotent by sequence, so repeating a partly written batch cannot duplicate events.
            }
        }
    }
}
