using System.Runtime.CompilerServices;
using System.Threading.Channels;
using GeoConquest.Core.Interfaces;

namespace GeoConquest.Web.BackgroundServices;

/// <summary>
/// An in-memory queue of game ids, built on a <see cref="Channel{T}"/>. It remembers which games are waiting and which
/// are being played so that a game is never played by two workers at once, yet a request that arrives mid-play is not lost.
/// </summary>
public sealed class NpcTurnQueue : INpcTurnQueue
{
    private readonly Channel<string> _channel = Channel.CreateUnbounded<string>();
    private readonly object _gate = new();
    private readonly HashSet<string> _waiting = [];
    private readonly HashSet<string> _running = [];
    private readonly HashSet<string> _runAgain = [];

    public void Enqueue(string gameId)
    {
        lock (_gate)
        {
            if (_running.Contains(gameId))
            {
                _runAgain.Add(gameId);
                return;
            }

            if (!_waiting.Add(gameId))
            {
                return;
            }
        }

        _channel.Writer.TryWrite(gameId);
    }

    /// <summary>Hands out game ids as they arrive. A game counts as running from the moment it is handed out until <see cref="Finished"/>.</summary>
    public async IAsyncEnumerable<string> DequeueAllAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var gameId in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            lock (_gate)
            {
                _waiting.Remove(gameId);
                _running.Add(gameId);
            }

            yield return gameId;
        }
    }

    /// <summary>Marks the game as no longer being played, and queues it again if it was requested meanwhile.</summary>
    public void Finished(string gameId)
    {
        bool requestedMeanwhile;
        lock (_gate)
        {
            _running.Remove(gameId);
            requestedMeanwhile = _runAgain.Remove(gameId);
        }

        if (requestedMeanwhile)
        {
            Enqueue(gameId);
        }
    }
}
