using Ardalis.Specification;

namespace FSH.Framework.Core.Specifications;

public class ListSpecification<T, TDto> : Specification<T, TDto> where T : class where TDto : class
{
    public ListSpecification(int pageNumber = 1, int pageSize = 10)
    {
        if (pageNumber <= 0) pageNumber = 1;

        if (pageSize <= 0) pageSize = 10;

        if (pageNumber > 1) Query.Skip((pageNumber - 1) * pageSize);

        Query.Take(pageSize).AsNoTracking();
    }
}
