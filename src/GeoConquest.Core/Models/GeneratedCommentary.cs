using GeoConquest.Core.Enums;

namespace GeoConquest.Core.Models;

/// <summary>A line of commentary and who wrote it.</summary>
public sealed record GeneratedCommentary(string Text, CommentarySource Source);
