using GeoConquest.Core.Models;

namespace GeoConquest.Core.Interfaces;

/// <summary>
/// The single entry point for playing a game. Every method validates that the game is in progress, that it is the
/// given player's turn and that the action is legal in the current <see cref="Enums.TurnPhase"/>, then updates the
/// game and returns the events describing what happened (in order, ready to be stored and shown).
/// Human and NPC players act through exactly the same methods.
/// </summary>
/// <exception cref="Exceptions.GameRuleException">Thrown by every action that breaks a rule; the message is user-facing.</exception>
public interface IGameEngine
{
    /// <summary>Creates a game with the human's chosen country plus nine random ones and starts the first turn.</summary>
    GameCreationResult CreateGame(string ownerUserId, string ownerDisplayName, string humanCountryCode, IReadOnlyList<Country> catalogue);

    /// <summary>Places this turn's reinforcements (round 2 onwards). Must be done before anything else in the turn.</summary>
    IReadOnlyList<GameEvent> AllocateReinforcements(Game game, string playerId, IReadOnlyDictionary<string, int> allocations);

    /// <summary>Moves soldiers between two of the player's countries. Each country keeps at least one soldier. Not allowed mid-attack.</summary>
    IReadOnlyList<GameEvent> Transfer(Game game, string playerId, string fromCountryCode, string toCountryCode, int soldiers);

    /// <summary>Begins the turn's single attack. The attacker needs 2+ soldiers and the target must belong to another player.</summary>
    IReadOnlyList<GameEvent> StartAttack(Game game, string playerId, string attackerCountryCode, string defenderCountryCode);

    /// <summary>Rolls one round of the attack in progress. Ends the attack on conquest or failure, and may end the game.</summary>
    IReadOnlyList<GameEvent> RollAttack(Game game, string playerId);

    /// <summary>Stops the attack in progress, which ends the turn immediately.</summary>
    IReadOnlyList<GameEvent> StopAttack(Game game, string playerId);

    /// <summary>Passes the turn without attacking.</summary>
    IReadOnlyList<GameEvent> Skip(Game game, string playerId);

    /// <summary>Ends the turn after an attack has been resolved (conquest or failure).</summary>
    IReadOnlyList<GameEvent> EndTurn(Game game, string playerId);

    /// <summary>
    /// Logs why an NPC is about to act, as a one-line justification. Changes no game state; it only takes the next
    /// place in the event log so the explanation sits right before the move it explains.
    /// </summary>
    GameEvent RecordNpcDecision(Game game, string playerId, string justification);
}
