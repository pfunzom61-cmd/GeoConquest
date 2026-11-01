using GeoConquest.Core.Constants;
using GeoConquest.Core.Enums;
using GeoConquest.Core.Exceptions;
using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;

namespace GeoConquest.Core.Services;

/// <inheritdoc />
public sealed class GameEngine(
    IGameSetupService setupService,
    IBattleService battleService,
    IReinforcementService reinforcementService,
    TimeProvider timeProvider) : IGameEngine
{
    public GameCreationResult CreateGame(
        string ownerUserId, string ownerDisplayName, string humanCountryCode, IReadOnlyList<Country> catalogue)
    {
        var game = setupService.CreateGame(ownerUserId, ownerDisplayName, humanCountryCode, catalogue);
        var humanCountryName = game.GetTerritory(humanCountryCode).CountryName;

        var events = new List<GameEvent>
        {
            Log(game, GameEventType.GameCreated, null, $"{ownerDisplayName} takes command of {humanCountryName}.", null),
        };
        BeginTurn(game, events);

        return new GameCreationResult(game, events);
    }

    public IReadOnlyList<GameEvent> AllocateReinforcements(
        Game game, string playerId, IReadOnlyDictionary<string, int> allocations)
    {
        var player = RequireTurn(game, playerId, TurnPhase.Reinforce);

        reinforcementService.Allocate(game, playerId, allocations);

        return allocations.Select(allocation => Log(
            game,
            GameEventType.Reinforced,
            player.Id,
            $"{Wording.PlayerName(player)} places {Wording.Count(allocation.Value, "soldier")} in {game.GetTerritory(allocation.Key).CountryName}.",
            new GameEventPayload { ToCountryCode = allocation.Key, Soldiers = allocation.Value })).ToList();
    }

    public IReadOnlyList<GameEvent> Transfer(
        Game game, string playerId, string fromCountryCode, string toCountryCode, int soldiers)
    {
        var player = RequireTurn(game, playerId, TurnPhase.Action, TurnPhase.PostAttack);

        if (soldiers < 1)
        {
            throw new GameRuleException("Transfer at least one soldier.");
        }

        if (fromCountryCode == toCountryCode)
        {
            throw new GameRuleException("Choose two different countries to move soldiers between.");
        }

        var source = game.GetTerritory(fromCountryCode);
        var destination = game.GetTerritory(toCountryCode);

        if (source.OwnerPlayerId != playerId || destination.OwnerPlayerId != playerId)
        {
            throw new GameRuleException("You can only move soldiers between countries you own.");
        }

        if (source.Soldiers - soldiers < 1)
        {
            throw new GameRuleException($"{source.CountryName} must keep at least 1 soldier.");
        }

        source.RemoveSoldiers(soldiers);
        destination.AddSoldiers(soldiers);

        return
        [
            Log(
                game,
                GameEventType.Transferred,
                player.Id,
                $"{Wording.PlayerName(player)} moves {Wording.Count(soldiers, "soldier")} from {source.CountryName} to {destination.CountryName}.",
                new GameEventPayload { FromCountryCode = fromCountryCode, ToCountryCode = toCountryCode, Soldiers = soldiers }),
        ];
    }

    public IReadOnlyList<GameEvent> StartAttack(
        Game game, string playerId, string attackerCountryCode, string defenderCountryCode)
    {
        var player = RequireTurn(game, playerId, TurnPhase.Action);

        var attackingTerritory = game.GetTerritory(attackerCountryCode);
        var defendingTerritory = game.GetTerritory(defenderCountryCode);

        if (attackingTerritory.OwnerPlayerId != playerId)
        {
            throw new GameRuleException("You can only attack from a country you own.");
        }

        if (defendingTerritory.OwnerPlayerId == playerId)
        {
            throw new GameRuleException("You cannot attack your own country.");
        }

        if (attackingTerritory.Soldiers < GameRules.MinimumAttackingSoldiers)
        {
            throw new GameRuleException(
                $"{attackingTerritory.CountryName} needs at least {GameRules.MinimumAttackingSoldiers} soldiers to attack.");
        }

        game.BeginBattle(attackerCountryCode, defenderCountryCode);

        return
        [
            Log(
                game,
                GameEventType.AttackStarted,
                player.Id,
                $"{Wording.PlayerName(player)} attacks {Wording.HeldCountry(defendingTerritory, game.GetPlayer(defendingTerritory.OwnerPlayerId))} " +
                $"({Wording.Count(defendingTerritory.Soldiers, "soldier")}) from {attackingTerritory.CountryName} ({Wording.Count(attackingTerritory.Soldiers, "soldier")}).",
                new GameEventPayload { FromCountryCode = attackerCountryCode, ToCountryCode = defenderCountryCode }),
        ];
    }

    public IReadOnlyList<GameEvent> RollAttack(Game game, string playerId)
    {
        var player = RequireTurn(game, playerId, TurnPhase.Attacking);
        var battle = game.ActiveBattle!;
        var attackingTerritory = game.GetTerritory(battle.AttackerCountryCode);
        var defendingTerritory = game.GetTerritory(battle.DefenderCountryCode);
        var defendingPlayer = game.GetPlayer(defendingTerritory.OwnerPlayerId);

        var attackerSoldiersBefore = attackingTerritory.Soldiers;
        var defenderSoldiersBefore = defendingTerritory.Soldiers;

        var round = battleService.RollRound(game);

        var events = new List<GameEvent>
        {
            Log(
                game,
                GameEventType.DiceRolled,
                player.Id,
                $"{attackingTerritory.CountryName} rolls [{string.Join(", ", round.AttackerDice)}] against " +
                $"{defendingTerritory.CountryName} [{string.Join(", ", round.DefenderDice)}]: " +
                $"attacker loses {round.AttackerLosses}, defender loses {round.DefenderLosses}.",
                new GameEventPayload
                {
                    FromCountryCode = battle.AttackerCountryCode,
                    ToCountryCode = battle.DefenderCountryCode,
                    AttackerDice = round.AttackerDice,
                    DefenderDice = round.DefenderDice,
                    AttackerLosses = round.AttackerLosses,
                    DefenderLosses = round.DefenderLosses,
                    AttackerSoldiersBefore = attackerSoldiersBefore,
                    AttackerSoldiersAfter = attackingTerritory.Soldiers,
                    DefenderSoldiersBefore = defenderSoldiersBefore,
                    DefenderSoldiersAfter = defenderSoldiersBefore - round.DefenderLosses,
                    BattleRoundNumber = battle.Rounds.Count,
                    GameRoundNumber = game.RoundNumber,
                    BattleAttackerLosses = battle.Rounds.Sum(fought => fought.AttackerLosses),
                    BattleDefenderLosses = battle.Rounds.Sum(fought => fought.DefenderLosses),
                    Outcome = round.Outcome,
                }),
        };

        game.NextEventSequence(); // the slot GameEvent.ReservedCommentarySequence points at, saved with this roll

        switch (round.Outcome)
        {
            case BattleOutcome.Failed:
                game.FinishBattle();
                events.Add(Log(
                    game,
                    GameEventType.AttackFailed,
                    player.Id,
                    $"{Wording.PlayerName(player)}'s attack on {defendingTerritory.CountryName} fails; " +
                    $"{attackingTerritory.CountryName} is down to 1 soldier.",
                    new GameEventPayload { FromCountryCode = battle.AttackerCountryCode, ToCountryCode = battle.DefenderCountryCode }));
                break;

            case BattleOutcome.Conquered:
                game.FinishBattle();
                events.Add(Log(
                    game,
                    GameEventType.CountryConquered,
                    player.Id,
                    $"{Wording.PlayerName(player)} conquers {defendingTerritory.CountryName} from {Wording.PlayerName(defendingPlayer)} and garrisons it with {Wording.Count(round.ConquestReward, "soldier")}.",
                    new GameEventPayload
                    {
                        FromCountryCode = battle.AttackerCountryCode,
                        ToCountryCode = battle.DefenderCountryCode,
                        Soldiers = round.ConquestReward,
                    }));
                ResolveConquestConsequences(game, player, defendingPlayer, events);
                break;
        }

        return events;
    }

    public IReadOnlyList<GameEvent> StopAttack(Game game, string playerId)
    {
        var player = RequireTurn(game, playerId, TurnPhase.Attacking);

        var events = new List<GameEvent>
        {
            Log(game, GameEventType.AttackStopped, player.Id, $"{Wording.PlayerName(player)} calls off the attack and ends the turn.", null),
        };
        game.FinishBattle();
        AdvanceTurn(game, events);

        return events;
    }

    public IReadOnlyList<GameEvent> Skip(Game game, string playerId)
    {
        var player = RequireTurn(game, playerId, TurnPhase.Action);

        var events = new List<GameEvent>
        {
            Log(game, GameEventType.TurnSkipped, player.Id, $"{Wording.PlayerName(player)} skips the turn.", null),
        };
        AdvanceTurn(game, events);

        return events;
    }

    public IReadOnlyList<GameEvent> EndTurn(Game game, string playerId)
    {
        RequireTurn(game, playerId, TurnPhase.PostAttack);

        var events = new List<GameEvent>();
        AdvanceTurn(game, events);

        return events;
    }

    public GameEvent RecordNpcDecision(Game game, string playerId, string justification)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(justification);

        var player = RequireTurn(game, playerId, TurnPhase.Reinforce, TurnPhase.Action, TurnPhase.Attacking, TurnPhase.PostAttack);
        if (player.IsHuman)
        {
            throw new GameRuleException("Only NPC decisions are recorded with a justification.");
        }

        return Log(game, GameEventType.NpcDecision, player.Id, justification, null);
    }

    /// <summary>Handles what a conquest can cause: the loser being eliminated, and the game being won or lost.</summary>
    private void ResolveConquestConsequences(Game game, Player conqueror, Player defeatedOwner, List<GameEvent> events)
    {
        if (game.TerritoriesOwnedBy(defeatedOwner.Id).Count == 0)
        {
            defeatedOwner.Eliminate();
            events.Add(Log(
                game, GameEventType.PlayerEliminated, defeatedOwner.Id, $"{Wording.PlayerName(defeatedOwner)} has been eliminated.", null));
        }

        if (game.Territories.All(territory => territory.OwnerPlayerId == conqueror.Id))
        {
            // Assumption 7: the human owning all ten wins; an NPC owning all ten ends the game as a loss.
            if (conqueror.IsHuman)
            {
                game.Finish(GameStatus.Won, conqueror.Id);
                events.Add(Log(game, GameEventType.GameWon, conqueror.Id, $"{Wording.PlayerName(conqueror)} has conquered the world!", null));
            }
            else
            {
                game.Finish(GameStatus.Lost, conqueror.Id);
                events.Add(Log(game, GameEventType.GameLost, conqueror.Id, $"{Wording.PlayerName(conqueror)} has conquered the world. Game over.", null));
            }
        }
        else if (defeatedOwner.IsHuman && defeatedOwner.IsEliminated)
        {
            game.Finish(GameStatus.Lost, null);
            events.Add(Log(game, GameEventType.GameLost, defeatedOwner.Id, $"{Wording.PlayerName(defeatedOwner)} has been eliminated. Game over.", null));
        }
    }

    private void AdvanceTurn(Game game, List<GameEvent> events)
    {
        game.AdvanceToNextLivingPlayer();
        BeginTurn(game, events);
    }

    private void BeginTurn(Game game, List<GameEvent> events)
    {
        var player = game.CurrentPlayer;
        var reinforcements = reinforcementService.CalculateReinforcements(
            game.TerritoriesOwnedBy(player.Id).Count, game.RoundNumber);

        game.StartTurn(reinforcements);

        var message = $"Round {game.RoundNumber}: {Wording.PlayerName(player)} to play.";
        if (reinforcements > 0)
        {
            message += $" {Wording.Count(reinforcements, "reinforcement")} to place.";
        }

        events.Add(Log(game, GameEventType.TurnStarted, player.Id, message, null));
    }

    private static Player RequireTurn(Game game, string playerId, params TurnPhase[] allowedPhases)
    {
        if (game.Status != GameStatus.InProgress)
        {
            throw new GameRuleException("The game is over.");
        }

        var player = game.GetPlayer(playerId);

        if (game.CurrentPlayer.Id != playerId)
        {
            throw new GameRuleException("It is not your turn.");
        }

        if (!allowedPhases.Contains(game.Phase))
        {
            throw new GameRuleException(game.Phase switch
            {
                TurnPhase.Reinforce => "Place your reinforcements first.",
                TurnPhase.Action => "Attack or skip your turn first.",
                TurnPhase.Attacking => "Finish or stop the attack in progress first.",
                _ => "You have already attacked this turn.",
            });
        }

        return player;
    }

    private GameEvent Log(Game game, GameEventType type, string? actorPlayerId, string message, GameEventPayload? payload)
    {
        var now = timeProvider.GetUtcNow();
        game.Touch(now);
        return new GameEvent(game.Id, game.NextEventSequence(), type, actorPlayerId, message, payload, now);
    }
}
