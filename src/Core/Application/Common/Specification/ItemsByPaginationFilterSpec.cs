using Ardalis.Specification;
using DN.WebApi.Application.Common.Models;

namespace DN.WebApi.Application.Common.Specification;

public class ItemsByPaginationFilterSpec<T> : ItemsByBaseFilterSpec<T>
{
    public ItemsByPaginationFilterSpec(PaginationFilter filter)
        : base(filter)
    {
        if (filter.OrderBy?.Any() is true)
        {
            Query.OrderBy(filter.OrderBy);
        }

        if (filter.PageNumber <= 0)
        {
            filter.PageNumber = 1;
        }

        if (filter.PageSize <= 0)
        {
            filter.PageSize = 10;
        }

        if (filter.PageNumber > 1)
        {
            Query.Skip((filter.PageNumber - 1) * filter.PageSize);
        }

        Query.Take(filter.PageSize);
    }
}