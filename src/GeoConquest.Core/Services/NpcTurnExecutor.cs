using GeoConquest.Core.Enums;
using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;

namespace GeoConquest.Core.Services;

/// <summary>
/// Plays an NPC's turn: asks the <see cref="INpcStrategy"/> what to do, logs its justification, then carries the
/// decision out through the <see cref="IGameEngine"/>. The order is reinforce, transfer, attack round by round, then end the turn.
/// </summary>
public sealed class NpcTurnExecutor(IGameEngine gameEngine, INpcStrategy strategy) : INpcTurnRunner
{
    public IReadOnlyList<GameEvent> PlayTurn(Game game)
    {
        var npc = game.CurrentPlayer;
        var events = new List<GameEvent>();

        if (game.Phase == TurnPhase.Reinforce)
        {
            var reinforcement = strategy.DecideReinforcements(game, npc);
            events.Add(gameEngine.RecordNpcDecision(game, npc.Id, reinforcement.Justification));
            events.AddRange(gameEngine.AllocateReinforcements(game, npc.Id, reinforcement.Allocations));
        }

        var transfer = strategy.DecideTransferBeforeAttack(game, npc);
        if (transfer is not null)
        {
            events.AddRange(Transfer(game, npc, transfer));
        }

        var attack = strategy.DecideAttack(game, npc);
        events.Add(gameEngine.RecordNpcDecision(game, npc.Id, attack.Justification));

        if (!attack.ShouldAttack)
        {
            events.AddRange(gameEngine.Skip(game, npc.Id));
            return events;
        }

        events.AddRange(gameEngine.StartAttack(game, npc.Id, attack.AttackerCountryCode!, attack.DefenderCountryCode!));

        while (game.Phase == TurnPhase.Attacking)
        {
            events.AddRange(gameEngine.RollAttack(game, npc.Id));
            if (game.Phase != TurnPhase.Attacking)
            {
                break;
            }

            var continuation = strategy.DecideContinueAttack(game, npc);
            events.Add(gameEngine.RecordNpcDecision(game, npc.Id, continuation.Justification));
            if (!continuation.KeepRolling)
            {
                events.AddRange(gameEngine.StopAttack(game, npc.Id));
                return events;
            }
        }

        if (game.Status != GameStatus.InProgress)
        {
            return events;
        }

        var conqueredTerritory = game.GetTerritory(attack.DefenderCountryCode!);
        if (conqueredTerritory.OwnerPlayerId == npc.Id)
        {
            var balancing = strategy.DecideTransferAfterConquest(game, npc, attack.AttackerCountryCode!, attack.DefenderCountryCode!);
            if (balancing is not null)
            {
                events.AddRange(Transfer(game, npc, balancing));
            }
        }

        events.AddRange(gameEngine.EndTurn(game, npc.Id));
        return events;
    }

    private List<GameEvent> Transfer(Game game, Player npc, TransferDecision transfer)
    {
        var events = new List<GameEvent> { gameEngine.RecordNpcDecision(game, npc.Id, transfer.Justification) };
        events.AddRange(gameEngine.Transfer(game, npc.Id, transfer.FromCountryCode, transfer.ToCountryCode, transfer.Soldiers));
        return events;
    }
}
