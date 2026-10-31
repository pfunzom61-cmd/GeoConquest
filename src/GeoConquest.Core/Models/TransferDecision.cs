namespace GeoConquest.Core.Models;

/// <summary>An NPC's decision to move soldiers between two of its countries, and why.</summary>
public sealed record TransferDecision(string FromCountryCode, string ToCountryCode, int Soldiers, string Justification);
