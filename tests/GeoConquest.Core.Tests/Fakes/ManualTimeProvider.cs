namespace GeoConquest.Core.Tests.Fakes;

/// <summary>A clock that only moves when a test moves it.</summary>
public sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
{
    private DateTimeOffset _now = now;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan duration) => _now += duration;
}
