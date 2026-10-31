namespace GeoConquest.Core.Models;

/// <summary>An NPC's decision, after a round, whether to keep rolling or call the attack off.</summary>
public sealed record ContinueAttackDecision(bool KeepRolling, string Justification);
