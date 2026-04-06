namespace UniKnowledge.DTOs.Shared;

/// <summary>
/// Generic cursor-based pagination response wrapper.
/// </summary>
public class CursorPagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public PageInfo PageInfo { get; set; } = new();
}

/// <summary>
/// Pagination metadata for cursor-based pagination.
/// </summary>
public class PageInfo
{
    public bool HasNextPage { get; set; }
    public string? EndCursor { get; set; }
}
