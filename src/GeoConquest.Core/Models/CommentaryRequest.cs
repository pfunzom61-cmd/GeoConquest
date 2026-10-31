namespace GeoConquest.Core.Models;

/// <summary>A round waiting for its commentary.</summary>
/// <param name="GameId">The game the round belongs to.</param>
/// <param name="Sequence">The log position the engine reserved for this commentary.</param>
/// <param name="ActorPlayerId">The player who rolled the dice.</param>
/// <param name="Context">The facts of the round.</param>
/// <param name="Deadline">When the line must be ready. Time spent waiting in the queue counts against it.</param>
public sealed record CommentaryRequest(
    string GameId,
    int Sequence,
    string? ActorPlayerId,
    BattleContext Context,
    DateTimeOffset Deadline);
