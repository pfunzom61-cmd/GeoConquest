namespace GeoConquest.Core.Models;

/// <summary>
/// How long to wait between NPC steps so people can follow the play. Bound from the "NpcPacing" configuration section;
/// fast-forward changes only these delays, never which steps are played or which events are logged.
/// </summary>
public sealed class NpcPacingOptions
{
    public const string SectionName = "NpcPacing";

    public int NormalDelayMilliseconds { get; set; } = 1200;

    public int FastForwardDelayMilliseconds { get; set; } = 200;

    public TimeSpan NormalDelay => TimeSpan.FromMilliseconds(NormalDelayMilliseconds);

    public TimeSpan FastForwardDelay => TimeSpan.FromMilliseconds(FastForwardDelayMilliseconds);
}
