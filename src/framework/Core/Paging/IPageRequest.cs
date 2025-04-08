namespace FSH.Framework.Core.Paging;

/// <summary>
/// Represents a paginated query request with optional filtering and sorting.
/// </summary>
public interface IPageRequest
{
    /// <summary>Page number (1-based).</summary>
    int PageNumber { get; init; }

    /// <summary>Number of items per page.</summary>
    int PageSize { get; init; }

    /// <summary>Optional filter expression (raw or JSON).</summary>
    string? Filters { get; init; }

    /// <summary>Optional sort order (e.g., "name asc", "created desc").</summary>
    string? SortOrder { get; init; }
}
