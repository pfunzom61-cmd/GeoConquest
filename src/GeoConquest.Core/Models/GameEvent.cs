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
}
