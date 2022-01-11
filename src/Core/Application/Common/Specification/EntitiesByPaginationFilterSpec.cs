using DN.WebApi.Application.Common.Models;

namespace DN.WebApi.Application.Common.Specification;

public class EntitiesByPaginationFilterSpec<T, TResult> : EntitiesByBaseFilterSpec<T, TResult>
{
    public EntitiesByPaginationFilterSpec(PaginationFilter filter)
        : base(filter) =>
        Query.PaginateBy(filter);
}

public class ItemsByPaginationFilterSpec<T> : ItemsByBaseFilterSpec<T>
{
    public ItemsByPaginationFilterSpec(PaginationFilter filter)
        : base(filter) =>
        Query.PaginateBy(filter);
}