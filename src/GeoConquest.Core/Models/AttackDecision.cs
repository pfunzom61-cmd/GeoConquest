namespace GeoConquest.Core.Models;

/// <summary>An NPC's decision about this turn's attack. Both country codes are null when it decides to hold.</summary>
public sealed record AttackDecision(string? AttackerCountryCode, string? DefenderCountryCode, string Justification)
{
    public bool ShouldAttack => AttackerCountryCode is not null && DefenderCountryCode is not null;
}
