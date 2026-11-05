namespace GeoConquest.Core.Tests.Fakes;

/// <summary>Time provider that always reports the same instant.</summary>
public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
