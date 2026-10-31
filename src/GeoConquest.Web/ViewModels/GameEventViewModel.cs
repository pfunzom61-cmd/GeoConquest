using GeoConquest.Core.Models;

namespace GeoConquest.Web.ViewModels;

/// <summary>
/// One line of the event log. The message already spells out dice, losses and outcomes; the other fields let the board
/// draw them. <see cref="ActorPlayerId"/> is the in-game player (such as "human" or "npc-BR"), never an account id.
/// </summary>
public sealed record GameEventViewModel(
    int Sequence,
    string Type,
    string Message,
    string? ActorPlayerId,
    string? FromCountryCode,
    string? ToCountryCode,
    IReadOnlyList<int>? AttackerDice,
    IReadOnlyList<int>? DefenderDice,
    int? AttackerLosses,
    int? DefenderLosses,
    string? CommentarySource,
    DateTimeOffset CreatedAt)
{
    public static GameEventViewModel From(GameEvent gameEvent) =>
        new(
            gameEvent.Sequence,
            gameEvent.Type.ToString(),
            gameEvent.Message,
            gameEvent.ActorPlayerId,
            gameEvent.Payload?.FromCountryCode,
            gameEvent.Payload?.ToCountryCode,
            gameEvent.Payload?.AttackerDice,
            gameEvent.Payload?.DefenderDice,
            gameEvent.Payload?.AttackerLosses,
            gameEvent.Payload?.DefenderLosses,
            gameEvent.Payload?.CommentarySource?.ToString(),
            gameEvent.CreatedAt);
}
