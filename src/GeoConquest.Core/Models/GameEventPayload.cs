namespace GeoConquest.Core.Models;

/// <summary>Structured details of a <see cref="GameEvent"/>. Only the fields relevant to the event type are set.</summary>
public sealed class GameEventPayload
{
    public string? FromCountryCode { get; init; }

    public string? ToCountryCode { get; init; }

    public int? Soldiers { get; init; }

    public IReadOnlyList<int>? AttackerDice { get; init; }

    public IReadOnlyList<int>? DefenderDice { get; init; }

    public int? AttackerLosses { get; init; }

    public int? DefenderLosses { get; init; }
}
