using GeoConquest.Core.Models;

namespace GeoConquest.Core.Interfaces;

/// <summary>Builds the opening position of a new game.</summary>
public interface IGameSetupService
{
    /// <summary>
    /// Creates a game with the human's chosen country plus nine other distinct countries picked at random.
    /// Each country starts with its population tier's soldiers and is owned by its own player: the human, or one NPC.
    /// The turn order is a random shuffle of all ten players.
    /// </summary>
    /// <exception cref="Exceptions.GameRuleException">The chosen country is not in the catalogue, or it has fewer than ten countries.</exception>
    Game CreateGame(string ownerUserId, string ownerDisplayName, string humanCountryCode, IReadOnlyList<Country> catalogue);
}
