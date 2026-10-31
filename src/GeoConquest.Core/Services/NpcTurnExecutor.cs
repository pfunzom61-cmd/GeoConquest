using GeoConquest.Core.Enums;
using GeoConquest.Core.Exceptions;
using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;

namespace GeoConquest.Core.Services;

/// <summary>
/// Plays an NPC's turn one step at a time: asks the <see cref="INpcStrategy"/> what to do, logs its justification,
/// then carries the decision out through the <see cref="IGameEngine"/>. The executor keeps no state of its own. The
/// next step is worked out from the game's turn phase alone, so a turn can be paused, saved and picked up again.
/// </summary>
public sealed class NpcTurnExecutor(IGameEngine gameEngine, INpcStrategy strategy) : INpcTurnRunner
{
    public IReadOnlyList<GameEvent> PlayStep(Game game)
    {
        if (game.Status != GameStatus.InProgress)
        {
            throw new GameRuleException("The game is over.");
        }

        var npc = game.CurrentPlayer;
        if (npc.IsHuman)
        {
            throw new GameRuleException("It is not an NPC's turn.");
        }

        return game.Phase switch
        {
            TurnPhase.Reinforce => Reinforce(game, npc),
            TurnPhase.Action => PrepareOrStartAttack(game, npc),
            TurnPhase.Attacking => RollOnce(game, npc),
            _ => gameEngine.EndTurn(game, npc.Id),
        };
    }

    private List<GameEvent> Reinforce(Game game, Player npc)
    {
        var reinforcement = strategy.DecideReinforcements(game, npc);
        var events = new List<GameEvent> { gameEngine.RecordNpcDecision(game, npc.Id, reinforcement.Justification) };
        events.AddRange(gameEngine.AllocateReinforcements(game, npc.Id, reinforcement.Allocations));
        return events;
    }

    /// <summary>
    /// Either moves soldiers to open up an attack, or decides on the attack. After a transfer the phase is still
    /// Action, so the next step asks again; the strategy only proposes a transfer while no attack is yet possible.
    /// </summary>
    private List<GameEvent> PrepareOrStartAttack(Game game, Player npc)
    {
        var transfer = strategy.DecideTransferBeforeAttack(game, npc);
        if (transfer is not null)
        {
            return Transfer(game, npc, transfer);
        }

        var attack = strategy.DecideAttack(game, npc);
        var events = new List<GameEvent> { gameEngine.RecordNpcDecision(game, npc.Id, attack.Justification) };

        events.AddRange(attack.ShouldAttack
            ? gameEngine.StartAttack(game, npc.Id, attack.AttackerCountryCode!, attack.DefenderCountryCode!)
            : gameEngine.Skip(game, npc.Id));
        return events;
    }

    /// <summary>
    /// Rolls one round, then either decides whether to press on, or (when the attack just ended in a conquest)
    /// evens out the two countries. The countries are read first because ending the attack clears the battle.
    /// </summary>
    private List<GameEvent> RollOnce(Game game, Player npc)
    {
        var battle = game.ActiveBattle!;
        var (attackerCountryCode, defenderCountryCode) = (battle.AttackerCountryCode, battle.DefenderCountryCode);

        var events = new List<GameEvent>(gameEngine.RollAttack(game, npc.Id));

        if (game.Phase == TurnPhase.Attacking)
        {
            var continuation = strategy.DecideContinueAttack(game, npc);
            events.Add(gameEngine.RecordNpcDecision(game, npc.Id, continuation.Justification));
            if (!continuation.KeepRolling)
            {
                events.AddRange(gameEngine.StopAttack(game, npc.Id));
            }

            return events;
        }

        if (game.Status == GameStatus.InProgress && game.GetTerritory(defenderCountryCode).OwnerPlayerId == npc.Id)
        {
            var balancing = strategy.DecideTransferAfterConquest(game, npc, attackerCountryCode, defenderCountryCode);
            if (balancing is not null)
            {
                events.AddRange(Transfer(game, npc, balancing));
            }
        }

        return events;
    }

    private List<GameEvent> Transfer(Game game, Player npc, TransferDecision transfer)
    {
        var events = new List<GameEvent> { gameEngine.RecordNpcDecision(game, npc.Id, transfer.Justification) };
        events.AddRange(gameEngine.Transfer(game, npc.Id, transfer.FromCountryCode, transfer.ToCountryCode, transfer.Soldiers));
        return events;
    }
}
