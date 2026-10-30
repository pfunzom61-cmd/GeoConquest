namespace GeoConquest.Core.Models;

/// <summary>One page of a larger result set, with the total so the UI can render paging controls.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
