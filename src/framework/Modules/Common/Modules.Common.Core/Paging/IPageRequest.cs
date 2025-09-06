namespace FSH.Framework.Core.Paging;

/// <summary>
/// Represents a paginated query request with optional filtering and sorting.
/// </summary>
public interface IPageRequest
{
    /// <summary>Page number (1-based).</summary>
    int PageNumber { get; set; }

    /// <summary>Number of items per page.</summary>
    int PageSize { get; set; }

    /// <summary>Optional filter expression (raw or JSON).</summary>
    string? Filters { get; set; }

    /// <summary>Optional sort order (e.g., "name asc", "created desc").</summary>
    string? SortOrder { get; set; }
}