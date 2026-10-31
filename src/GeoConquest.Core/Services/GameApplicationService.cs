using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;

namespace GeoConquest.Core.Services;

/// <inheritdoc />
public sealed class GameApplicationService(
    IGameEngine gameEngine,
    IGameProgressRecorder progressRecorder,
    INpcTurnQueue npcTurnQueue,
    ICountryRepository countryRepository) : IGameApplicationService
{
    public async Task<Game> CreateGameAsync(
        string ownerUserId, string ownerDisplayName, string humanCountryCode, CancellationToken cancellationToken)
    {
        var catalogue = await countryRepository.GetAllAsync(cancellationToken);
        var creation = gameEngine.CreateGame(ownerUserId, ownerDisplayName, humanCountryCode, catalogue);

        try
        {
            await progressRecorder.RecordNewGameAsync(creation.Game, creation.Events, cancellationToken);
        }
        finally
        {
            QueueNpcTurns(creation.Game);
        }

        return creation.Game;
    }

    public Task<IReadOnlyList<GameEvent>> AllocateReinforcementsAsync(
        Game game, IReadOnlyDictionary<string, int> allocations, CancellationToken cancellationToken) =>
        ApplyAsync(game, humanId => gameEngine.AllocateReinforcements(game, humanId, allocations), cancellationToken);

    public Task<IReadOnlyList<GameEvent>> TransferAsync(
        Game game, string fromCountryCode, string toCountryCode, int soldiers, CancellationToken cancellationToken) =>
        ApplyAsync(game, humanId => gameEngine.Transfer(game, humanId, fromCountryCode, toCountryCode, soldiers), cancellationToken);

    public Task<IReadOnlyList<GameEvent>> StartAttackAsync(
        Game game, string attackerCountryCode, string defenderCountryCode, CancellationToken cancellationToken) =>
        ApplyAsync(game, humanId => gameEngine.StartAttack(game, humanId, attackerCountryCode, defenderCountryCode), cancellationToken);

    public Task<IReadOnlyList<GameEvent>> RollAttackAsync(Game game, CancellationToken cancellationToken) =>
        ApplyAsync(game, humanId => gameEngine.RollAttack(game, humanId), cancellationToken);

    public Task<IReadOnlyList<GameEvent>> StopAttackAsync(Game game, CancellationToken cancellationToken) =>
        ApplyAsync(game, humanId => gameEngine.StopAttack(game, humanId), cancellationToken);

    public Task<IReadOnlyList<GameEvent>> SkipAsync(Game game, CancellationToken cancellationToken) =>
        ApplyAsync(game, humanId => gameEngine.Skip(game, humanId), cancellationToken);

    public Task<IReadOnlyList<GameEvent>> EndTurnAsync(Game game, CancellationToken cancellationToken) =>
        ApplyAsync(game, humanId => gameEngine.EndTurn(game, humanId), cancellationToken);

    private async Task<IReadOnlyList<GameEvent>> ApplyAsync(
        Game game, Func<string, IReadOnlyList<GameEvent>> humanAction, CancellationToken cancellationToken)
    {
        var events = humanAction(game.HumanPlayer.Id);

        try
        {
            await progressRecorder.RecordProgressAsync(game, events, cancellationToken);
        }
        finally
        {
            // Even if writing the log failed after the game was saved, the NPCs must still get their turn. If the game
            // was not saved at all, the runner finds it still on the human's turn and does nothing.
            QueueNpcTurns(game);
        }

        return events;
    }

    private void QueueNpcTurns(Game game)
    {
        if (game.AwaitsNpcTurn)
        {
            npcTurnQueue.Enqueue(game.Id);
        }
    }
}
