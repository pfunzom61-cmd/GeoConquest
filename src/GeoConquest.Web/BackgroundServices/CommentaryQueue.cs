using System.Threading.Channels;
using GeoConquest.Core.Interfaces;
using GeoConquest.Core.Models;

namespace GeoConquest.Web.BackgroundServices;

/// <summary>
/// An in-memory queue of rounds waiting for commentary. Requests are small and every one finishes within its time
/// budget, so it stays short. Requests still queued at a restart are lost, which only leaves those rounds without a line.
/// </summary>
public sealed class CommentaryQueue : ICommentaryQueue
{
    private readonly Channel<CommentaryRequest> _channel = Channel.CreateUnbounded<CommentaryRequest>();

    public void Enqueue(CommentaryRequest request) => _channel.Writer.TryWrite(request);

    public IAsyncEnumerable<CommentaryRequest> DequeueAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
