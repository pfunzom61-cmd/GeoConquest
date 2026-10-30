using GeoConquest.Core.Models;

namespace GeoConquest.Core.Interfaces;

/// <summary>Stores the append-only log of what happened in each game.</summary>
public interface IGameEventRepository
{
    Task AppendAsync(IReadOnlyCollection<GameEvent> events, CancellationToken cancellationToken);

    /// <summary>Returns the game's events with a sequence greater than <paramref name="afterSequence"/>, oldest first.</summary>
    Task<IReadOnlyList<GameEvent>> GetAfterSequenceAsync(string gameId, int afterSequence, CancellationToken cancellationToken);

    /// <summary>Returns one page of the game's events, newest first. Pages start at 1.</summary>
    Task<PagedResult<GameEvent>> GetPageAsync(string gameId, int page, int pageSize, CancellationToken cancellationToken);
}
