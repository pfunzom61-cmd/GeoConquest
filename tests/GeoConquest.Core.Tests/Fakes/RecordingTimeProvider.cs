namespace GeoConquest.Core.Tests.Fakes;

/// <summary>A time provider whose delays finish at once but are written down, so a test can check how long code asked to wait.</summary>
public sealed class RecordingTimeProvider : TimeProvider
{
    public List<TimeSpan> Delays { get; } = [];

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        Delays.Add(dueTime);
        ThreadPool.QueueUserWorkItem(_ => callback(state));
        return new FinishedTimer();
    }

    private sealed class FinishedTimer : ITimer
    {
        public bool Change(TimeSpan dueTime, TimeSpan period) => true;

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
