using GeoConquest.Core.Models;

namespace GeoConquest.Core.Interfaces;

/// <summary>
/// Runs one use case per call: apply the human's action through the engine, let the NPCs play until it is the human's
/// turn again, save the game, then append the events. Callers load the game and check the user may play it first.
/// </summary>
/// <exception cref="Exceptions.GameRuleException">The action breaks a rule; nothing is saved.</exception>
/// <exception cref="Exceptions.ConcurrencyException">The game was changed elsewhere since it was loaded; nothing is saved.</exception>
public interface IGameApplicationService
{
    /// <summary>Creates and stores a game for the owner, with NPCs playing until the owner's first turn.</summary>
    Task<Game> CreateGameAsync(string ownerUserId, string ownerDisplayName, string humanCountryCode, CancellationToken cancellationToken);

    Task<IReadOnlyList<GameEvent>> AllocateReinforcementsAsync(Game game, IReadOnlyDictionary<string, int> allocations, CancellationToken cancellationToken);

    Task<IReadOnlyList<GameEvent>> TransferAsync(Game game, string fromCountryCode, string toCountryCode, int soldiers, CancellationToken cancellationToken);

    Task<IReadOnlyList<GameEvent>> StartAttackAsync(Game game, string attackerCountryCode, string defenderCountryCode, CancellationToken cancellationToken);

    Task<IReadOnlyList<GameEvent>> RollAttackAsync(Game game, CancellationToken cancellationToken);

    Task<IReadOnlyList<GameEvent>> StopAttackAsync(Game game, CancellationToken cancellationToken);

    Task<IReadOnlyList<GameEvent>> SkipAsync(Game game, CancellationToken cancellationToken);

    Task<IReadOnlyList<GameEvent>> EndTurnAsync(Game game, CancellationToken cancellationToken);
}
