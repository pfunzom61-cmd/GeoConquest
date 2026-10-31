namespace GeoConquest.Core.Exceptions;

/// <summary>
/// Thrown when the language model did not produce commentary. <see cref="RetryAfter"/> is set when further requests
/// are pointless for a while, such as after a rate-limit response or a rejected API key.
/// </summary>
public sealed class CommentaryProviderException(string message, TimeSpan? retryAfter = null) : Exception(message)
{
    public TimeSpan? RetryAfter { get; } = retryAfter;
}
