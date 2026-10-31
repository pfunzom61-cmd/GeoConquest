using GeoConquest.Core.Enums;
using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;

namespace GeoConquest.Core.Services;

/// <summary>
/// Queues commentary for every dice round in a saved step and sets how long each may take. Nothing waits for the
/// result: the log position is already reserved, so commentary that lands after the next step still reads in order.
/// </summary>
public sealed class BattleCommentaryScheduler(ICommentaryQueue queue, CommentaryOptions options, TimeProvider timeProvider)
{
    public void ScheduleFor(Game game, IReadOnlyList<GameEvent> events)
    {
        foreach (var diceRolled in events.Where(gameEvent => gameEvent.Type == GameEventType.DiceRolled))
        {
            queue.Enqueue(new CommentaryRequest(
                game.Id,
                diceRolled.ReservedCommentarySequence,
                diceRolled.ActorPlayerId,
                BattleContext.FromRoll(game, diceRolled),
                timeProvider.GetUtcNow() + BudgetFor(game, diceRolled)));
        }
    }

    /// <summary>An NPC's fast-forwarded rolls come too quickly to be worth a request each, so they get no time for the model.</summary>
    private TimeSpan BudgetFor(Game game, GameEvent diceRolled) =>
        game.IsFastForward && !game.GetPlayer(diceRolled.ActorPlayerId!).IsHuman ? TimeSpan.Zero : options.TimeBudget;
}
