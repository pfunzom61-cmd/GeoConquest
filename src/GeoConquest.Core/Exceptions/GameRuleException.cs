namespace GeoConquest.Core.Exceptions;

/// <summary>
/// Thrown when an action breaks a game rule. The message is written for the player and is safe to show in the UI.
/// </summary>
public sealed class GameRuleException(string message) : Exception(message);
