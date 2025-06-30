using FSH.Framework.Core.Paging;

namespace FSH.Framework.Core.Specifications;

public class EntitiesByPaginationFilterSpec<T, TResult> : EntitiesByBaseFilterSpec<T, TResult>
{
    protected PaginationFilter PaginationFilter { get; }
    public EntitiesByPaginationFilterSpec(PaginationFilter filter)
        : base(filter)
    {
        PaginationFilter = filter;
    }
    public void ConfigurePaginationQuery()
    {
        base.ConfigureQuery();
        Query.PaginateBy(PaginationFilter);
    }
}

public class EntitiesByPaginationFilterSpec<T> : EntitiesByBaseFilterSpec<T>
{
    protected PaginationFilter PaginationFilter { get; }
    public EntitiesByPaginationFilterSpec(PaginationFilter filter)
        : base(filter)
    {
        PaginationFilter = filter;
    }
    public void ConfigurePaginationQuery()
    {
        base.ConfigureQuery();
        Query.PaginateBy(PaginationFilter);
    }
}
