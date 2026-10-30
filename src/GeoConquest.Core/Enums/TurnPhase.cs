namespace GeoConquest.Core.Enums;

/// <summary>Where the current player is within their turn. Drives which actions are legal.</summary>
public enum TurnPhase
{
    /// <summary>Reinforcements must be allocated before anything else (round 2 onwards).</summary>
    Reinforce,

    /// <summary>Transfers are allowed; the player may start an attack or skip.</summary>
    Action,

    /// <summary>An attack is in progress; the player may roll or stop. Transfers are blocked.</summary>
    Attacking,

    /// <summary>The attack ended in conquest or failure; transfers are allowed, then the turn ends.</summary>
    PostAttack,

    /// <summary>The game is finished; no further actions are accepted.</summary>
    GameOver,
}
