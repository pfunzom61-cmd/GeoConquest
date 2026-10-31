using GeoConquest.Core.Enums;
using GeoConquest.Core.Models;

namespace GeoConquest.Core.Interfaces;

/// <summary>Read-only lookups for the pages: a single game, a user's campaigns, public live games and the recent event log.</summary>
public interface IGameQueryService
{
    Task<Game?> GetGameAsync(string gameId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the user's games. <paramref name="search"/> matches the owner's name or any of the game's country names,
    /// ignoring case; a null or blank search matches everything.
    /// </summary>
    Task<IReadOnlyList<Game>> ListCampaignsAsync(
        string ownerUserId, string? search, GameStatus? status, CampaignSort sort, CancellationToken cancellationToken);

    /// <summary>Returns games in progress, most recently active first (at most <see cref="IGameRepository.MaxInProgressGames"/>), filtered by the same search rule.</summary>
    Task<IReadOnlyList<Game>> ListLiveGamesAsync(string? search, CancellationToken cancellationToken);

    /// <summary>Returns the game's most recent events in the order they happened (oldest first).</summary>
    Task<IReadOnlyList<GameEvent>> GetRecentEventsAsync(string gameId, int count, CancellationToken cancellationToken);
}
