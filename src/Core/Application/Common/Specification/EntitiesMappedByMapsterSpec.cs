using Ardalis.Specification;
using Mapster;

namespace DN.WebApi.Application.Common.Specification;

public class EntitiesMappedByMapsterSpec<T, TResult> : Specification<T, TResult>
{
    public EntitiesMappedByMapsterSpec() =>
        Query.Select(item => item!.Adapt<TResult>());
}