using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;

namespace GeoConquest.Web.Tests.Fakes;

/// <summary>Keeps the event log in memory. Like the real one, it ignores an event whose sequence is already stored.</summary>
public sealed class InMemoryGameEventRepository : IGameEventRepository
{
    private readonly List<GameEvent> _events = [];

    public Task AppendAsync(IReadOnlyCollection<GameEvent> events, CancellationToken cancellationToken)
    {
        lock (_events)
        {
            foreach (var gameEvent in events.Where(candidate => !_events.Any(stored =>
                         stored.GameId == candidate.GameId && stored.Sequence == candidate.Sequence)))
            {
                _events.Add(gameEvent);
            }
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<GameEvent>> GetAfterSequenceAsync(string gameId, int afterSequence, CancellationToken cancellationToken)
    {
        lock (_events)
        {
            return Task.FromResult<IReadOnlyList<GameEvent>>(_events
                .Where(gameEvent => gameEvent.GameId == gameId && gameEvent.Sequence > afterSequence)
                .OrderBy(gameEvent => gameEvent.Sequence)
                .ToList());
        }
    }

    public Task<PagedResult<GameEvent>> GetPageAsync(string gameId, int page, int pageSize, CancellationToken cancellationToken)
    {
        lock (_events)
        {
            var forThisGame = _events.Where(gameEvent => gameEvent.GameId == gameId).OrderByDescending(gameEvent => gameEvent.Sequence).ToList();
            var items = forThisGame.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult(new PagedResult<GameEvent>(items, forThisGame.Count, page, pageSize));
        }
    }
}
