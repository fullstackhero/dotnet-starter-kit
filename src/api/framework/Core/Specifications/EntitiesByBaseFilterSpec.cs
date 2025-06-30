using Ardalis.Specification;
using FSH.Framework.Core.Paging;

namespace FSH.Framework.Core.Specifications;

public class EntitiesByBaseFilterSpec<T, TResult> : Specification<T, TResult>
{
    protected BaseFilter Filter { get; }
    public EntitiesByBaseFilterSpec(BaseFilter filter)
    {
        Filter = filter;
    }
    public void ConfigureQuery() => Query.SearchBy(Filter);
}

public class EntitiesByBaseFilterSpec<T> : Specification<T>
{
    protected BaseFilter Filter { get; }
    public EntitiesByBaseFilterSpec(BaseFilter filter)
    {
        Filter = filter;
    }
    public void ConfigureQuery() => Query.SearchBy(Filter);
}
