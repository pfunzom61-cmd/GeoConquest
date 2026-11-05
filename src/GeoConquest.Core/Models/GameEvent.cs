using GeoConquest.Core.Enums;

namespace GeoConquest.Core.Models;

/// <summary>One entry in a game's append-only log: what happened, who did it, and a readable message.</summary>
public sealed class GameEvent(
    string gameId,
    int sequence,
    GameEventType type,
    string? actorPlayerId,
    string message,
    GameEventPayload? payload,
    DateTimeOffset createdAt)
{
    public string GameId { get; } = gameId;

    /// <summary>Position in the game's log, starting at 1 and strictly increasing.</summary>
    public int Sequence { get; } = sequence;

    public GameEventType Type { get; } = type;

    public string? ActorPlayerId { get; } = actorPlayerId;

    public string Message { get; } = message;

    public GameEventPayload? Payload { get; } = payload;

    public DateTimeOffset CreatedAt { get; } = createdAt;

    /// <summary>
    /// For a <see cref="GameEventType.DiceRolled"/> event: the log position the engine reserved for that round's
    /// commentary. Commentary is written after the roll is saved, and reserving its place means writing it needs no
    /// second save of the game, so it can never conflict with the player's next move.
    /// </summary>
    public int ReservedCommentarySequence => Sequence + 1;
}
