namespace FSH.Framework.Core.Paging;

public class PaginationFilter : BaseFilter, IPageRequest
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public IReadOnlyList<string>? OrderBy { get; init; }

    public string? Filters { get; init; }
    public string? SortOrder { get; init; }
}
