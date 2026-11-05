using GeoConquest.Core.Models;

namespace GeoConquest.Core.Interfaces;

/// <summary>Produces the commentary for one queued round, stores it in its reserved place in the log and tells the viewers.</summary>
public interface ICommentaryProcessor
{
    Task ProcessAsync(CommentaryRequest request, CancellationToken cancellationToken);
}
