using GeoConquest.Core.Enums;
using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;

namespace GeoConquest.Core.Services;

/// <inheritdoc />
public sealed class BattleCommentaryProcessor(
    ICommentaryService commentaryService,
    IGameProgressRecorder progressRecorder,
    TimeProvider timeProvider) : ICommentaryProcessor
{
    public async Task ProcessAsync(CommentaryRequest request, CancellationToken cancellationToken)
    {
        var timeLeft = request.Deadline - timeProvider.GetUtcNow();
        var commentary = await commentaryService.DescribeRoundAsync(
            request.Context, timeLeft > TimeSpan.Zero ? timeLeft : TimeSpan.Zero, cancellationToken);

        await progressRecorder.RecordCommentaryAsync(new GameEvent(
            request.GameId,
            request.Sequence,
            GameEventType.Commentary,
            request.ActorPlayerId,
            commentary.Text,
            new GameEventPayload { CommentarySource = commentary.Source },
            timeProvider.GetUtcNow()));
    }
}
