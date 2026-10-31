using GeoConquest.Core.Enums;
using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;

namespace GeoConquest.Core.Services;

/// <inheritdoc />
public sealed class GameApplicationService(
    IGameEngine gameEngine,
    INpcTurnRunner npcTurnRunner,
    IGameProgressRecorder progressRecorder,
    ICountryRepository countryRepository) : IGameApplicationService
{
    public async Task<Game> CreateGameAsync(
        string ownerUserId, string ownerDisplayName, string humanCountryCode, CancellationToken cancellationToken)
    {
        var catalogue = await countryRepository.GetAllAsync(cancellationToken);
        var creation = gameEngine.CreateGame(ownerUserId, ownerDisplayName, humanCountryCode, catalogue);

        var events = new List<GameEvent>(creation.Events);
        PlayNpcTurnsUntilHumanTurn(creation.Game, events);

        await progressRecorder.RecordNewGameAsync(creation.Game, events, cancellationToken);
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
        var events = new List<GameEvent>(humanAction(game.HumanPlayer.Id));
        PlayNpcTurnsUntilHumanTurn(game, events);

        await progressRecorder.RecordProgressAsync(game, events, cancellationToken);
        return events;
    }

    private void PlayNpcTurnsUntilHumanTurn(Game game, List<GameEvent> events)
    {
        while (game.Status == GameStatus.InProgress && !game.CurrentPlayer.IsHuman)
        {
            events.AddRange(npcTurnRunner.PlayStep(game));
        }
    }
}
