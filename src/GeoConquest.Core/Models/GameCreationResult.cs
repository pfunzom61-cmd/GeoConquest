namespace GeoConquest.Core.Models;

/// <summary>A newly created game together with the events that describe its setup.</summary>
public sealed record GameCreationResult(Game Game, IReadOnlyList<GameEvent> Events);
