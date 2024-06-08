using Ardalis.Specification;
using FSH.Framework.Core.Paging;

namespace FSH.Framework.Core.Specifications;

public class ListSpecification<T, TDto> : Specification<T, TDto> where T : class where TDto : class
{
    public ListSpecification(PaginationFilter filter)
    {
        if (filter.PageNumber <= 0) filter.PageNumber = 1;

        if (filter.PageSize <= 0) filter.PageSize = 10;

        if (filter.PageNumber > 1) Query.Skip((filter.PageNumber - 1) * filter.PageSize);

        Query.Take(filter.PageSize).AsNoTracking();
    }
}

