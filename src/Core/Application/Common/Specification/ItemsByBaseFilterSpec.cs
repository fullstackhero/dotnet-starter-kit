using Ardalis.Specification;
using DN.WebApi.Application.Common.Models;

namespace DN.WebApi.Application.Common.Specification;

public class ItemsByBaseFilterSpec<T> : Specification<T>
{
    public ItemsByBaseFilterSpec(BaseFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            Query.SearchByKeyword(filter.Keyword);
        }

        if (filter.AdvancedSearch?.Keyword is not null)
        {
            Query.AdvancedSearch(filter.AdvancedSearch);
        }
    }
}