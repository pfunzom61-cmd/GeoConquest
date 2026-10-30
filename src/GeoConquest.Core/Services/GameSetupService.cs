using GeoConquest.Core.Constants;
using GeoConquest.Core.Exceptions;
using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;

namespace GeoConquest.Core.Services;

/// <inheritdoc />
public sealed class GameSetupService(
    ISoldierAllocator soldierAllocator,
    IShuffler shuffler,
    TimeProvider timeProvider) : IGameSetupService
{
    private const string HumanPlayerId = "human";

    public Game CreateGame(string ownerUserId, string ownerDisplayName, string humanCountryCode, IReadOnlyList<Country> catalogue)
    {
        var availableCountries = catalogue.DistinctBy(country => country.IsoCode).ToList();

        var humanCountry = availableCountries.FirstOrDefault(country => country.IsoCode == humanCountryCode)
            ?? throw new GameRuleException("Please choose a country from the list.");

        if (availableCountries.Count < GameRules.CountriesPerGame)
        {
            throw new GameRuleException(
                $"At least {GameRules.CountriesPerGame} countries are needed to start a game.");
        }

        var npcCountries = shuffler
            .Shuffle(availableCountries.Where(country => country != humanCountry).ToList())
            .Take(GameRules.CountriesPerGame - 1)
            .ToList();

        var players = new List<Player> { new(HumanPlayerId, ownerDisplayName, true, humanCountry.IsoCode, ColorKeyFor(0)) };
        players.AddRange(npcCountries.Select((country, index) =>
            new Player(NpcPlayerId(country), country.Name, false, country.IsoCode, ColorKeyFor(index + 1))));

        var territories = new List<Country> { humanCountry }.Concat(npcCountries)
            .Select((country, index) =>
            {
                var soldiers = soldierAllocator.GetSoldierValue(country.Population);
                return new Territory(country.IsoCode, country.Name, soldiers, players[index].Id, soldiers);
            })
            .ToList();

        var turnOrder = shuffler.Shuffle(players.Select(player => player.Id).ToList());

        return new Game(
            Guid.NewGuid().ToString("N"),
            ownerUserId,
            ownerDisplayName,
            players,
            territories,
            turnOrder,
            timeProvider.GetUtcNow());
    }

    private static string NpcPlayerId(Country country) => $"npc-{country.IsoCode}";

    private static string ColorKeyFor(int playerIndex) => $"color-{playerIndex}";
}
