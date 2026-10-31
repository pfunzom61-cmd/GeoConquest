using GeoConquest.Core.Enums;
using GeoConquest.Core.Exceptions;

namespace GeoConquest.Core.Models;

/// <summary>
/// Aggregate root for one game: players, board, turn order and the current turn's state.
/// State only changes through the methods below, which protect the turn invariants. The rules
/// themselves (who may do what) live in the engine services.
/// </summary>
public sealed class Game
{
    private readonly List<Player> _players;
    private readonly List<Territory> _territories;
    private readonly List<string> _turnOrder;

    public Game(
        string id,
        string ownerUserId,
        string ownerDisplayName,
        IEnumerable<Player> players,
        IEnumerable<Territory> territories,
        IEnumerable<string> turnOrder,
        DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerDisplayName);

        _players = [.. players];
        _territories = [.. territories];
        _turnOrder = [.. turnOrder];

        if (_players.Count == 0 || _territories.Count == 0)
        {
            throw new ArgumentException("A game needs at least one player and one territory.");
        }

        if (!_players.Select(player => player.Id).Order().SequenceEqual(_turnOrder.Order()))
        {
            throw new ArgumentException("The turn order must list every player exactly once.", nameof(turnOrder));
        }

        Id = id;
        OwnerUserId = ownerUserId;
        OwnerDisplayName = ownerDisplayName;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
        Status = GameStatus.InProgress;
        RoundNumber = 1;
        Phase = TurnPhase.Action;
    }

    public string Id { get; }

    public string OwnerUserId { get; }

    public string OwnerDisplayName { get; }

    public GameStatus Status { get; private set; }

    public IReadOnlyList<Player> Players => _players;

    public IReadOnlyList<Territory> Territories => _territories;

    /// <summary>Player ids in the order they take turns.</summary>
    public IReadOnlyList<string> TurnOrder => _turnOrder;

    public int CurrentTurnIndex { get; private set; }

    /// <summary>Round 1 ends once every player has had one turn.</summary>
    public int RoundNumber { get; private set; }

    public TurnPhase Phase { get; private set; }

    /// <summary>Soldiers the current player must still place before doing anything else.</summary>
    public int PendingReinforcements { get; private set; }

    public BattleState? ActiveBattle { get; private set; }

    /// <summary>
    /// Whether the owner has asked for NPC turns to play at the short pace. It only changes the delay between steps and
    /// is switched off again when the turn reaches the human (assessment assumption 10).
    /// </summary>
    public bool IsFastForward { get; private set; }

    public string? WinnerPlayerId { get; private set; }

    /// <summary>Sequence number of the last event logged for this game.</summary>
    public int LastEventSequence { get; private set; }

    /// <summary>
    /// Optimistic-concurrency counter. The repository saves a game only if the stored version still matches,
    /// then calls <see cref="AdvanceVersion"/> so this instance mirrors what was written.
    /// </summary>
    public int Version { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Player CurrentPlayer => GetPlayer(_turnOrder[CurrentTurnIndex]);

    /// <summary>The owner's player. Every game has exactly one human; the others are NPCs.</summary>
    public Player HumanPlayer => _players.Single(player => player.IsHuman);

    /// <summary>True while the game is in progress and an NPC, not the human, is the one to act.</summary>
    public bool AwaitsNpcTurn => Status == GameStatus.InProgress && !CurrentPlayer.IsHuman;

    public Player GetPlayer(string playerId) =>
        _players.FirstOrDefault(player => player.Id == playerId)
        ?? throw new GameRuleException("That player is not part of this game.");

    public Territory GetTerritory(string countryCode) =>
        _territories.FirstOrDefault(territory => territory.CountryCode == countryCode)
        ?? throw new GameRuleException("That country is not part of this game.");

    public IReadOnlyList<Territory> TerritoriesOwnedBy(string playerId) =>
        _territories.Where(territory => territory.OwnerPlayerId == playerId).ToList();

    /// <summary>
    /// Starts the current player's turn. With reinforcements owed the phase is Reinforce, otherwise Action.
    /// When the human's turn begins, fast-forward is switched off ("until your next turn").
    /// </summary>
    public void StartTurn(int reinforcements)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(reinforcements);

        PendingReinforcements = reinforcements;
        ActiveBattle = null;
        Phase = reinforcements > 0 ? TurnPhase.Reinforce : TurnPhase.Action;

        if (CurrentPlayer.IsHuman)
        {
            IsFastForward = false;
        }
    }

    /// <exception cref="GameRuleException">The game is already over.</exception>
    public void SetFastForward(bool enabled)
    {
        if (Status != GameStatus.InProgress)
        {
            throw new GameRuleException("The game is over, so there is nothing left to speed up.");
        }

        IsFastForward = enabled;
    }

    /// <summary>Called once all reinforcements are placed.</summary>
    public void CompleteReinforcement()
    {
        PendingReinforcements = 0;
        Phase = TurnPhase.Action;
    }

    /// <summary>Moves to the next player who is still in the game, counting a new round each time the order wraps.</summary>
    public void AdvanceToNextLivingPlayer()
    {
        if (_players.All(player => player.IsEliminated))
        {
            throw new InvalidOperationException("No living players remain to take a turn.");
        }

        do
        {
            CurrentTurnIndex = (CurrentTurnIndex + 1) % _turnOrder.Count;
            if (CurrentTurnIndex == 0)
            {
                RoundNumber++;
            }
        }
        while (CurrentPlayer.IsEliminated);
    }

    public void BeginBattle(string attackerCountryCode, string defenderCountryCode)
    {
        if (Phase != TurnPhase.Action)
        {
            throw new GameRuleException("An attack can only be started at the start of your action phase.");
        }

        ActiveBattle = new BattleState(attackerCountryCode, defenderCountryCode);
        Phase = TurnPhase.Attacking;
    }

    /// <summary>Ends the attack after conquest or failure; the player may still transfer before ending the turn.</summary>
    public void FinishBattle()
    {
        ActiveBattle = null;
        Phase = TurnPhase.PostAttack;
    }

    public void Finish(GameStatus status, string? winnerPlayerId)
    {
        if (status == GameStatus.InProgress)
        {
            throw new ArgumentException("A finished game cannot be in progress.", nameof(status));
        }

        Status = status;
        WinnerPlayerId = winnerPlayerId;
        ActiveBattle = null;
        PendingReinforcements = 0;
        Phase = TurnPhase.GameOver;
    }

    /// <summary>Reserves the next number in the event log.</summary>
    public int NextEventSequence() => ++LastEventSequence;

    public void Touch(DateTimeOffset now) => UpdatedAt = now;

    /// <summary>Records that the repository has saved this game as the next version.</summary>
    public void AdvanceVersion() => Version++;
}
