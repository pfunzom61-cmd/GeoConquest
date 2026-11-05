namespace GeoConquest.Core.Models;

/// <summary>How an NPC places its reinforcements, and why. The allocation always sums to the pending reinforcements.</summary>
public sealed record ReinforcementDecision(IReadOnlyDictionary<string, int> Allocations, string Justification);
