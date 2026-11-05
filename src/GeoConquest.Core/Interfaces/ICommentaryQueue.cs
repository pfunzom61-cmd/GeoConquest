using GeoConquest.Core.Models;

namespace GeoConquest.Core.Interfaces;

/// <summary>Rounds waiting for commentary. Adding one returns at once; something else (the background worker) writes the line.</summary>
public interface ICommentaryQueue
{
    void Enqueue(CommentaryRequest request);
}
