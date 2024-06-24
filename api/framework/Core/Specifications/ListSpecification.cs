using System.Linq.Expressions;
using Ardalis.Specification;
using FSH.Framework.Core.Paging;

namespace FSH.Framework.Core.Specifications;

public class ListSpecification<T, TDto> : Specification<T, TDto> where T : class where TDto : class
{
    public ListSpecification(PaginationFilter filter)
    {
        ApplyPagination(filter.PageNumber, filter.PageSize);
        ApplySorting(filter.AdvancedFilter);
        ApplySearch(filter.AdvancedSearch);
        //ApplyAdvancedFilter(filter.AdvancedFilter);
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

        var parameter = Expression.Parameter(typeof(T), "x");
        var property = Expression.Property(parameter, advancedFilter.Field);
        var lambda = Expression.Lambda<Func<T, object>>(Expression.Convert(property, typeof(object)), parameter);

        switch (advancedFilter.Operator)
        {
            case FilterOperator.EQ: // Utilisé pour OrderBy
            case FilterOperator.LT: // Utilisé pour OrderBy
            case FilterOperator.LTE: // Utilisé pour OrderBy
            case FilterOperator.GT: // Utilisé pour OrderBy
            case FilterOperator.GTE: // Utilisé pour OrderBy
                Query.OrderBy(lambda);
                break;
            case FilterOperator.NEQ: // Utilisé pour OrderByDescending
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

    private void ApplyAdvancedFilter(Filter? advancedFilter)
    {
        if (advancedFilter == null) return;

        var parameter = Expression.Parameter(typeof(T), "x");
        var body = CreateFilterExpression(parameter, advancedFilter);

        if (body == null) return;

        var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);
        Query.Where(lambda);
    }

    private Expression? CreateFilterExpression(ParameterExpression parameter, Filter filter)
    {
        Expression? body = null;

        if (filter.Filters != null && filter.Filters.Any() || filter.Logic != null)
        {
            foreach (var subFilter in filter.Filters)
            {
                var subExpression = CreateFilterExpression(parameter, subFilter);
                if (subExpression == null) continue;

                body = body == null ? subExpression :
                       filter.Logic switch
                       {
                           FilterLogic.AND => Expression.AndAlso(body, subExpression),
                           FilterLogic.OR => Expression.OrElse(body, subExpression),
                           FilterLogic.XOR => Expression.ExclusiveOr(body, subExpression),
                           _ => throw new NotSupportedException($"Logic '{filter.Logic}' is not supported.")
                       };
            }
        }
        else if (filter.Field != null && filter.Operator != null)
        {
            var property = Expression.Property(parameter, filter.Field);
            var constant = Expression.Constant(Convert.ChangeType(filter.Value, property.Type));

            body = filter.Operator switch
            {
                FilterOperator.EQ => Expression.Equal(property, constant),
                FilterOperator.NEQ => Expression.NotEqual(property, constant),
                FilterOperator.LT => Expression.LessThan(property, constant),
                FilterOperator.LTE => Expression.LessThanOrEqual(property, constant),
                FilterOperator.GT => Expression.GreaterThan(property, constant),
                FilterOperator.GTE => Expression.GreaterThanOrEqual(property, constant),
                FilterOperator.STARTSWITH => Expression.Call(property, typeof(string).GetMethod("StartsWith", new[] { typeof(string) })!, constant),
                FilterOperator.ENDSWITH => Expression.Call(property, typeof(string).GetMethod("EndsWith", new[] { typeof(string) })!, constant),
                FilterOperator.CONTAINS => Expression.Call(property, typeof(string).GetMethod("Contains", new[] { typeof(string) })!, constant),
                _ => throw new NotSupportedException($"Operator '{filter.Operator}' is not supported.")
            };
        }

        return body;
    }
}
