namespace FSH.Framework.Core.Paging;

public class PaginationFilter : BaseFilter, IPageRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string[] OrderBy { get; set; }

    public string? Filters { get; set; }
    public string? SortOrder { get; set; }
}