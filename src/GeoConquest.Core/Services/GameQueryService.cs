using GeoConquest.Core.Enums;
using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;

namespace GeoConquest.Core.Services;

/// <inheritdoc />
public sealed class GameQueryService(IGameRepository gameRepository, IGameEventRepository gameEventRepository) : IGameQueryService
{
    public Task<Game?> GetGameAsync(string gameId, CancellationToken cancellationToken) =>
        gameRepository.GetByIdAsync(gameId, cancellationToken);

    public async Task<IReadOnlyList<Game>> ListCampaignsAsync(
        string ownerUserId, string? search, GameStatus? status, CampaignSort sort, CancellationToken cancellationToken)
    {
        var games = await gameRepository.ListByOwnerAsync(ownerUserId, status, cancellationToken);
        var matching = games.Where(game => Matches(game, search));

        return sort switch
        {
            CampaignSort.Newest => matching.OrderByDescending(game => game.CreatedAt).ToList(),
            CampaignSort.Country => matching.OrderBy(game => HumanCountryName(game), StringComparer.OrdinalIgnoreCase).ToList(),
            _ => matching.OrderByDescending(game => game.UpdatedAt).ToList(),
        };
    }

    public async Task<IReadOnlyList<Game>> ListLiveGamesAsync(string? search, CancellationToken cancellationToken)
    {
        var games = await gameRepository.ListInProgressAsync(cancellationToken);
        return games.Where(game => Matches(game, search)).ToList();
    }

    public async Task<IReadOnlyList<GameEvent>> GetRecentEventsAsync(string gameId, int count, CancellationToken cancellationToken)
    {
        var newestFirst = await gameEventRepository.GetPageAsync(gameId, page: 1, pageSize: count, cancellationToken);
        return newestFirst.Items.Reverse().ToList();
    }

    private static string HumanCountryName(Game game) => game.GetTerritory(game.HumanPlayer.HomeCountryCode).CountryName;

    private static bool Matches(Game game, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return true;
        }

        var term = search.Trim();
        return game.OwnerDisplayName.Contains(term, StringComparison.OrdinalIgnoreCase)
            || game.Territories.Any(territory => territory.CountryName.Contains(term, StringComparison.OrdinalIgnoreCase));
    }
}
