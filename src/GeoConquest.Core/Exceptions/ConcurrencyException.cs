namespace GeoConquest.Core.Exceptions;

/// <summary>
/// Thrown when a game was changed by someone else since it was loaded, so saving it would overwrite their change.
/// The caller should reload the game and try again.
/// </summary>
public sealed class ConcurrencyException(string message) : Exception(message);
