namespace FSH.WebApi.Application.Common.Models;

public abstract class PaginationFilter : BaseFilter
{
    protected PaginationFilter()
    {
        PageSize = int.MaxValue;
    }

    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public string[]? OrderBy { get; set; }
}