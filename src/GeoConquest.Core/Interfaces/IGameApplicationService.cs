using GeoConquest.Core.Models;

namespace GeoConquest.Core.Interfaces;

/// <summary>
/// Runs one use case per call: apply the human's action through the engine, then save the game, append the events and
/// tell the viewers. If an NPC is next, the game is queued for the background runner, which plays the NPC turns one
/// step at a time (see <see cref="INpcTurnProcessor"/>). Callers load the game and check the user may play it first.
/// </summary>
/// <exception cref="Exceptions.GameRuleException">The action breaks a rule; nothing is saved.</exception>
/// <exception cref="Exceptions.ConcurrencyException">The game was changed elsewhere since it was loaded; nothing is saved.</exception>
public interface IGameApplicationService
{
    /// <summary>Creates and stores a game for the owner. NPCs ahead of the owner in the turn order play in the background.</summary>
    Task<Game> CreateGameAsync(string ownerUserId, string ownerDisplayName, string humanCountryCode, CancellationToken cancellationToken);

    Task<IReadOnlyList<GameEvent>> AllocateReinforcementsAsync(Game game, IReadOnlyDictionary<string, int> allocations, CancellationToken cancellationToken);

    Task<IReadOnlyList<GameEvent>> TransferAsync(Game game, string fromCountryCode, string toCountryCode, int soldiers, CancellationToken cancellationToken);

    Task<IReadOnlyList<GameEvent>> StartAttackAsync(Game game, string attackerCountryCode, string defenderCountryCode, CancellationToken cancellationToken);

    Task<IReadOnlyList<GameEvent>> RollAttackAsync(Game game, CancellationToken cancellationToken);

    Task<IReadOnlyList<GameEvent>> StopAttackAsync(Game game, CancellationToken cancellationToken);

    Task<IReadOnlyList<GameEvent>> SkipAsync(Game game, CancellationToken cancellationToken);

    Task<IReadOnlyList<GameEvent>> EndTurnAsync(Game game, CancellationToken cancellationToken);

    /// <summary>
    /// Switches fast-forward on or off and tells the viewers. The NPC runner may save the game at any moment, so on a
    /// conflict the game is reloaded and the setting applied again. Returns the game as saved, which may be a newer copy than the one passed in.
    /// </summary>
    /// <exception cref="Exceptions.GameRuleException">The game is already over.</exception>
    Task<Game> SetFastForwardAsync(Game game, bool enabled, CancellationToken cancellationToken);
}
