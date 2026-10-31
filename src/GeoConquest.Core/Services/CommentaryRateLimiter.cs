using GeoConquest.Core.Models;

namespace GeoConquest.Core.Services;

/// <summary>
/// Keeps requests to the language model within the free tier: a sliding one-minute window, shared by every game,
/// plus a pause for when the provider itself says to wait. Being refused here simply means using a template.
/// </summary>
public sealed class CommentaryRateLimiter(CommentaryOptions options, TimeProvider timeProvider)
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    private readonly object _gate = new();
    private readonly Queue<DateTimeOffset> _recentRequests = new();
    private DateTimeOffset _pausedUntil = DateTimeOffset.MinValue;

    /// <summary>Claims one request if the pause is over and the last minute has room for it.</summary>
    public bool TryAcquire()
    {
        var now = timeProvider.GetUtcNow();

        lock (_gate)
        {
            while (_recentRequests.TryPeek(out var oldest) && now - oldest >= Window)
            {
                _recentRequests.Dequeue();
            }

            if (now < _pausedUntil || _recentRequests.Count >= options.MaxRequestsPerMinute)
            {
                return false;
            }

            _recentRequests.Enqueue(now);
            return true;
        }
    }

    public void PauseFor(TimeSpan duration)
    {
        var until = timeProvider.GetUtcNow() + duration;

        lock (_gate)
        {
            if (until > _pausedUntil)
            {
                _pausedUntil = until;
            }
        }
    }
}
