using System.Linq.Expressions;
using System.Reflection;

namespace FSH.WebApi.Application.Common.Specification;

// See https://github.com/ardalis/Specification/issues/53
public static class SpecificationBuilderExtensions
{
    public static ISpecificationBuilder<T> SearchBy<T>(this ISpecificationBuilder<T> query, BaseFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            query.SearchByKeyword(filter.Keyword);
        }

        if (filter.AdvancedSearch?.Keyword is not null)
        {
            query.AdvancedSearch(filter.AdvancedSearch);
        }

        return query;
    }

    public static ISpecificationBuilder<T> PaginateBy<T>(this ISpecificationBuilder<T> query, PaginationFilter filter)
    {
        if (filter.OrderBy?.Any() is true)
        {
            query.OrderBy(filter.OrderBy);
        }

        if (filter.PageNumber <= 0)
        {
            filter.PageNumber = 1;
        }

        if (filter.PageSize <= 0)
        {
            filter.PageSize = 10;
        }

        if (filter.PageNumber > 1)
        {
            query.Skip((filter.PageNumber - 1) * filter.PageSize);
        }

        query.Take(filter.PageSize);

        return query;
    }

    public static IOrderedSpecificationBuilder<T> SearchByKeyword<T>(
        this ISpecificationBuilder<T> specificationBuilder,
        string keyword) =>
        specificationBuilder.AdvancedSearch(new Search { Keyword = keyword });

    public static IOrderedSpecificationBuilder<T> AdvancedSearch<T>(
        this ISpecificationBuilder<T> specificationBuilder,
        Search search)
    {
        if (!string.IsNullOrEmpty(search.Keyword))
        {
            foreach (var property in typeof(T).GetProperties()
                        .Where(prop => prop.GetGetMethod()?.IsVirtual is not true &&
                                    (!search.Fields.Any() || search.Fields.Any(
                                        field => prop.Name.Equals(field, StringComparison.OrdinalIgnoreCase)))))
            {
                var paramExpr = Expression.Parameter(typeof(T));
                var propertyExpr = Expression.Property(paramExpr, property);

                Expression selectorExpr =
                    property.PropertyType == typeof(string)
                        ? propertyExpr
                        : Expression.Condition(
                            Expression.Equal(
                                Expression.Convert(propertyExpr, typeof(object)),
                                Expression.Constant(null, typeof(object))),
                            Expression.Constant(null, typeof(string)),
                            Expression.Call(propertyExpr, "ToString", null, null));

                var selector = Expression.Lambda<Func<T, string>>(selectorExpr, paramExpr);

                ((List<(Expression<Func<T, string>> Selector, string SearchTerm, int SearchGroup)>)specificationBuilder.Specification.SearchCriterias)
                    .Add((selector, $"%{search.Keyword}%", 1));
            }
        }

        return new OrderedSpecificationBuilder<T>(specificationBuilder.Specification);
    }

    public static IOrderedSpecificationBuilder<T> OrderBy<T>(
        this ISpecificationBuilder<T> specificationBuilder,
        string[] orderByFields)
    {
        var fields = ParseOrderBy(orderByFields);
        if (fields is not null)
        {
            foreach (var field in fields)
            {
                var matchedProperty = typeof(T).GetProperty(field.Key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (matchedProperty is null)
                    throw new ArgumentException($"OrderBy field '{field.Key}' doesn't have a corresponding property in type '{typeof(T).Name}'", nameof(orderByFields));

                var paramExpr = Expression.Parameter(typeof(T));
                var propertyExpr = Expression.Convert(
                    Expression.PropertyOrField(paramExpr, matchedProperty.Name), typeof(object));

                var keySelector = Expression.Lambda<Func<T, object?>>(propertyExpr, paramExpr);

                ((List<(Expression<Func<T, object?>> KeySelector, OrderTypeEnum OrderType)>)specificationBuilder.Specification.OrderExpressions)
                    .Add((keySelector, field.Value));
            }
        }

        return new OrderedSpecificationBuilder<T>(specificationBuilder.Specification);
    }

    private static IDictionary<string, OrderTypeEnum>? ParseOrderBy(string[] orderByFields) =>
        orderByFields is null
            ? null
            : new Dictionary<string, OrderTypeEnum>(
                orderByFields.Select((orderByfield, index) =>
                {
                    string[] fieldParts = orderByfield.Split(' ');
                    string field = fieldParts[0];
                    bool descending = fieldParts.Length > 1 && fieldParts[1].Equals("Descending", StringComparison.OrdinalIgnoreCase);
                    var orderBy = index == 0
                        ? descending ? OrderTypeEnum.OrderByDescending
                                     : OrderTypeEnum.OrderBy
                        : descending ? OrderTypeEnum.ThenByDescending
                                     : OrderTypeEnum.ThenBy;

                    return new KeyValuePair<string, OrderTypeEnum>(field, orderBy);
                }));
}