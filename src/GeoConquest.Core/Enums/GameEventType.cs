namespace GeoConquest.Core.Enums;

/// <summary>Kinds of entries in a game's append-only event log.</summary>
public enum GameEventType
{
    GameCreated,
    TurnStarted,
    Reinforced,
    Transferred,
    AttackStarted,
    DiceRolled,
    AttackStopped,
    AttackFailed,
    CountryConquered,
    PlayerEliminated,
    TurnSkipped,
    NpcDecision,
    GameWon,
    GameLost,
}
