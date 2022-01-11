using Ardalis.Specification;
using DN.WebApi.Application.Common.Models;

namespace DN.WebApi.Application.Common.Specification;

public class EntitiesByBaseFilterSpec<T, TResult> : EntitiesMappedByMapsterSpec<T, TResult>
{
    public EntitiesByBaseFilterSpec(BaseFilter filter) =>
        Query.SearchBy(filter);
}

public class ItemsByBaseFilterSpec<T> : Specification<T>
{
    public ItemsByBaseFilterSpec(BaseFilter filter) =>
        Query.SearchBy(filter);
}