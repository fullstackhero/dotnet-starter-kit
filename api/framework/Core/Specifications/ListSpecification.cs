using System.Linq.Expressions;
using Ardalis.Specification;
using FSH.Framework.Core.Paging;
using System.Linq;

namespace FSH.Framework.Core.Specifications;

public class ListSpecification<T, TDto> : Specification<T, TDto> where T : class where TDto : class
{
    public ListSpecification(PaginationFilter filter)
    {
        ApplyPagination(filter.PageNumber, filter.PageSize);
        ApplySorting(filter.AdvancedFilter);
        ApplySearch(filter.AdvancedSearch);
    }

    private void ApplyPagination(int pageNumber, int pageSize)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = 10;

        if (pageNumber > 1)
        {
            Query.Skip((pageNumber - 1) * pageSize);
        }

        Query.Take(pageSize).AsNoTracking();
    }

    private void ApplySorting(Filter? advancedFilter)
    {
        if (advancedFilter == null || string.IsNullOrWhiteSpace(advancedFilter.Field)) return;

        // Utilisation de réflexion pour accéder aux propriétés
        var parameter = Expression.Parameter(typeof(T), "x");
        var property = Expression.Property(parameter, advancedFilter.Field);
        var lambda = Expression.Lambda<Func<T, object>>(Expression.Convert(property, typeof(object)), parameter);

        switch (advancedFilter.Operator)
        {
            case FilterOperator.EQ: // Pour l'exemple, on utilise EQ pour OrderBy
                Query.OrderBy(lambda);
                break;
            case FilterOperator.NEQ: // Pour l'exemple, on utilise NEQ pour OrderByDescending
                Query.OrderByDescending(lambda);
                break;
            default:
                throw new NotSupportedException($"Operator '{advancedFilter.Operator}' is not supported for sorting.");
        }
    }

    private void ApplySearch(Search? advancedSearch)
    {
        if (advancedSearch == null || string.IsNullOrWhiteSpace(advancedSearch.Keyword)) return;

        var parameter = Expression.Parameter(typeof(T), "x");
        Expression? body = null;

        foreach (var field in advancedSearch.Fields)
        {
            var property = Expression.Property(parameter, field);
            var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
            var keywordExpression = Expression.Constant(advancedSearch.Keyword);
            var containsExpression = Expression.Call(property, containsMethod, keywordExpression);

            if (body == null)
            {
                body = containsExpression;
            }
            else
            {
                body = Expression.OrElse(body, containsExpression);
            }
        }

        if (body == null) return;

        var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);
        Query.Where(lambda);
    }
}
