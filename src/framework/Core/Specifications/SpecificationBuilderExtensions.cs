using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Ardalis.Specification;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Core.Paging;

namespace FSH.Framework.Core.Specifications;

// See https://github.com/ardalis/Specification/issues/53
public static class SpecificationBuilderExtensions
{
    public static ISpecificationBuilder<T> SearchBy<T>(this ISpecificationBuilder<T> query, BaseFilter filter) =>
        query
            .SearchByKeyword(filter.Keyword)
            .AdvancedSearch(filter.AdvancedSearch)
            .AdvancedFilter(filter.AdvancedFilter);

    public static ISpecificationBuilder<T> PaginateBy<T>(
        this ISpecificationBuilder<T> query,
        PaginationFilter filter)
    {
        var pageNumber = filter.PageNumber <= 0 ? 1 : filter.PageNumber;
        var pageSize = filter.PageSize <= 0 ? 10 : filter.PageSize;

        if (pageNumber > 1)
        {
            query = query.Skip((pageNumber - 1) * pageSize);
        }

        query = query.Take(pageSize);

        if (filter.OrderBy is { Count: > 0 })
        {
            query = query.OrderBy(filter.OrderBy);
        }

        return query;
    }



    public static IOrderedSpecificationBuilder<T> SearchByKeyword<T>(
        this ISpecificationBuilder<T> specificationBuilder,
        string? keyword) =>
        specificationBuilder.AdvancedSearch(new Search { Keyword = keyword });

    public static IOrderedSpecificationBuilder<T> AdvancedSearch<T>(
        this ISpecificationBuilder<T> specificationBuilder,
        Search? search)
    {
        if (!string.IsNullOrEmpty(search?.Keyword))
        {
            if (search.Fields?.Any() is true)
            {
                // search seleted fields (can contain deeper nested fields)
                foreach (string field in search.Fields)
                {
                    var paramExpr = Expression.Parameter(typeof(T));
                    MemberExpression propertyExpr = GetPropertyExpression(field, paramExpr);

                    specificationBuilder.AddSearchPropertyByKeyword(propertyExpr, paramExpr, search.Keyword);
                }
            }
            else
            {
                // search all fields (only first level)
                foreach (var property in typeof(T).GetProperties()
                    .Where(prop => (Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType) is { } propertyType
                        && !propertyType.IsEnum
                        && Type.GetTypeCode(propertyType) != TypeCode.Object))
                {
                    var paramExpr = Expression.Parameter(typeof(T));
                    var propertyExpr = Expression.Property(paramExpr, property);

                    specificationBuilder.AddSearchPropertyByKeyword(propertyExpr, paramExpr, search.Keyword);
                }
            }
        }

        return new OrderedSpecificationBuilder<T>(specificationBuilder.Specification);
    }

    private static void AddSearchPropertyByKeyword<T>(
        this ISpecificationBuilder<T> specificationBuilder,
        Expression propertyExpr,
        ParameterExpression paramExpr,
        string keyword,
        FilterOperator operatorSearch = FilterOperator.Contains)
    {
        if (propertyExpr is not MemberExpression memberExpr || memberExpr.Member is not PropertyInfo property)
        {
            throw new ArgumentException("propertyExpr must be a property expression.", nameof(propertyExpr));
        }

        var searchTerm = operatorSearch switch
        {
            FilterOperator.StartsWith => $"{keyword.ToLower()}%",
            FilterOperator.EndsWith => $"%{keyword.ToLower()}",
            FilterOperator.Contains => $"%{keyword.ToLower()}%",
            _ => throw new CustomException($"Unsupported filter operator: {operatorSearch}")
        };

        // Generate lambda [ x => x.Property ] for string properties
        // or [ x => ((object)x.Property) == null ? null : x.Property.ToString() ] for other properties
        Expression selectorExpr =
            property.PropertyType == typeof(string)
                ? propertyExpr
                : Expression.Condition(
                    Expression.Equal(Expression.Convert(propertyExpr, typeof(object)), Expression.Constant(null, typeof(object))),
                    Expression.Constant(null, typeof(string)),
                    Expression.Call(propertyExpr, "ToString", null, null));

        var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes);
        Expression callToLowerMethod = Expression.Call(selectorExpr, toLowerMethod!);

        var selector = Expression.Lambda<Func<T, string>>(callToLowerMethod, paramExpr);

        ((List<SearchExpressionInfo<T>>)specificationBuilder.Specification.SearchCriterias)
            .Add(new SearchExpressionInfo<T>(selector, searchTerm, 1));
    }

    public static IOrderedSpecificationBuilder<T> AdvancedFilter<T>(
        this ISpecificationBuilder<T> specificationBuilder,
        Filter? filter)
    {
        if (filter is not null)
        {
            var parameter = Expression.Parameter(typeof(T));

            Expression binaryExpresioFilter;

            if (!string.IsNullOrEmpty(filter.Logic))
            {
                if (filter.Filters is null) throw new CustomException("The Filters attribute is required when declaring a logic");
                binaryExpresioFilter = CreateFilterExpression(filter.Logic, filter.Filters, parameter);
            }
            else
            {
                var filterValid = GetValidFilter(filter);
                binaryExpresioFilter = CreateFilterExpression(filterValid.Field!, filterValid.Operator!, filterValid.Value, parameter);
            }

            ((List<WhereExpressionInfo<T>>)specificationBuilder.Specification.WhereExpressions)
                .Add(new WhereExpressionInfo<T>(Expression.Lambda<Func<T, bool>>(binaryExpresioFilter, parameter)));
        }

        return new OrderedSpecificationBuilder<T>(specificationBuilder.Specification);
    }

    private static Expression CreateFilterExpression(
        string logic,
        IEnumerable<Filter> filters,
        ParameterExpression parameter)
    {
        Expression filterExpression = default!;

        foreach (var filter in filters)
        {
            Expression bExpresionFilter;

            if (!string.IsNullOrEmpty(filter.Logic))
            {
                if (filter.Filters is null) throw new CustomException("The Filters attribute is required when declaring a logic");
                bExpresionFilter = CreateFilterExpression(filter.Logic, filter.Filters, parameter);
            }
            else
            {
                var filterValid = GetValidFilter(filter);
                bExpresionFilter = CreateFilterExpression(filterValid.Field!, filterValid.Operator!, filterValid.Value, parameter);
            }

            filterExpression = filterExpression is null ? bExpresionFilter : CombineFilter(logic, filterExpression, bExpresionFilter);
        }

        return filterExpression;
    }

    private static Expression CreateFilterExpression(
        string field,
        string filterOperator,
        object? value,
        ParameterExpression parameter)
    {
        var propertyExpresion = GetPropertyExpression(field, parameter);
        var valueExpresion = GeValuetExpression(field, value, propertyExpresion.Type);
        return CreateFilterExpression(propertyExpresion, valueExpresion, filterOperator);
    }

    private static Expression CreateFilterExpression(
        Expression memberExpression,
        Expression constantExpression,
        FilterOperator filterOperator)
    {
        if (memberExpression.Type == typeof(string))
        {
            constantExpression = Expression.Call(constantExpression, nameof(string.ToLower), null);
            memberExpression = Expression.Call(memberExpression, nameof(string.ToLower), null);
        }

        return filterOperator switch
        {
            FilterOperator.Eq => Expression.Equal(memberExpression, constantExpression),
            FilterOperator.Neq => Expression.NotEqual(memberExpression, constantExpression),
            FilterOperator.Lt => Expression.LessThan(memberExpression, constantExpression),
            FilterOperator.Lte => Expression.LessThanOrEqual(memberExpression, constantExpression),
            FilterOperator.Gt => Expression.GreaterThan(memberExpression, constantExpression),
            FilterOperator.Gte => Expression.GreaterThanOrEqual(memberExpression, constantExpression),
            FilterOperator.Contains => Expression.Call(memberExpression, nameof(string.Contains), null, constantExpression),
            FilterOperator.StartsWith => Expression.Call(memberExpression, nameof(string.StartsWith), null, constantExpression),
            FilterOperator.EndsWith => Expression.Call(memberExpression, nameof(string.EndsWith), null, constantExpression),
            _ => throw new CustomException("Filter operator is not valid.")
        };
    }


    private static Expression CombineFilter(
        FilterLogic logic,
        Expression left,
        Expression right)
    {
        return logic switch
        {
            FilterLogic.And => Expression.And(left, right),
            FilterLogic.Or => Expression.Or(left, right),
            FilterLogic.Xor => Expression.ExclusiveOr(left, right),
            _ => throw new ArgumentOutOfRangeException(nameof(logic), "Invalid filter logic.")
        };
    }


    private static MemberExpression GetPropertyExpression(
        string propertyName,
        ParameterExpression parameter)
    {
        Expression propertyExpression = parameter;
        foreach (string member in propertyName.Split('.'))
        {
            propertyExpression = Expression.PropertyOrField(propertyExpression, member);
        }

        return (MemberExpression)propertyExpression;
    }

    private static string GetStringFromJsonElement(object value)
        => ((JsonElement)value).GetString()!;

    private static ConstantExpression GeValuetExpression(
        string field,
        object? value,
        Type propertyType)
    {
        if (value == null) return Expression.Constant(null, propertyType);

        if (propertyType.IsEnum)
        {
            string? stringEnum = GetStringFromJsonElement(value);

            if (!Enum.TryParse(propertyType, stringEnum, true, out object? valueparsed)) throw new CustomException(string.Format("Value {0} is not valid for {1}", value, field));

            return Expression.Constant(valueparsed, propertyType);
        }

        if (propertyType == typeof(Guid))
        {
            string? stringGuid = GetStringFromJsonElement(value);

            if (!Guid.TryParse(stringGuid, out Guid valueparsed)) throw new CustomException(string.Format("Value {0} is not valid for {1}", value, field));

            return Expression.Constant(valueparsed, propertyType);
        }

        if (propertyType == typeof(string))
        {
            string? text = GetStringFromJsonElement(value);

            return Expression.Constant(text, propertyType);
        }

        if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
        {
            string? text = GetStringFromJsonElement(value);
            return Expression.Constant(ChangeType(text, propertyType), propertyType);
        }

        return Expression.Constant(ChangeType(((JsonElement)value).GetRawText(), propertyType), propertyType);
    }

    public static dynamic? ChangeType(object value, Type conversion)
    {
        var t = conversion;

        if (t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
        {
            if (value == null)
            {
                return null;
            }

            t = Nullable.GetUnderlyingType(t);
        }

        return Convert.ChangeType(value, t!);
    }

    private static Filter GetValidFilter(Filter filter)
    {
        if (string.IsNullOrWhiteSpace(filter.Field))
            throw new CustomException("The 'Field' property is required when declaring a filter.");

        // Optional: check if Operator is defined in the enum (defensive)
        if (!Enum.IsDefined(typeof(FilterOperator), filter.Operator!))
            throw new CustomException($"Invalid filter operator: {filter.Operator}");

        return filter;
    }

    public static IOrderedSpecificationBuilder<T> OrderBy<T>(
        this ISpecificationBuilder<T> specificationBuilder,
        string[]? orderByFields)
    {
        if (orderByFields is not null)
        {
            foreach (var field in ParseOrderBy(orderByFields))
            {
                var paramExpr = Expression.Parameter(typeof(T));

                Expression propertyExpr = paramExpr;
                foreach (string member in field.Key.Split('.'))
                {
                    propertyExpr = Expression.PropertyOrField(propertyExpr, member);
                }

                var keySelector = Expression.Lambda<Func<T, object?>>(
                    Expression.Convert(propertyExpr, typeof(object)),
                    paramExpr);

                ((List<OrderExpressionInfo<T>>)specificationBuilder.Specification.OrderExpressions)
                    .Add(new OrderExpressionInfo<T>(keySelector, field.Value));
            }
        }

        return new OrderedSpecificationBuilder<T>(specificationBuilder.Specification);
    }

    private static Dictionary<string, OrderTypeEnum> ParseOrderBy(string[] orderByFields) =>
        new(orderByFields.Select((orderByfield, index) =>
        {
            string[] fieldParts = orderByfield.Split(' ');
            string field = fieldParts[0];
            bool descending = fieldParts.Length > 1 && fieldParts[1].StartsWith("Desc", StringComparison.OrdinalIgnoreCase);
            var orderBy = index == 0
                ? descending ? OrderTypeEnum.OrderByDescending
                                : OrderTypeEnum.OrderBy
                : descending ? OrderTypeEnum.ThenByDescending
                                : OrderTypeEnum.ThenBy;

            return new KeyValuePair<string, OrderTypeEnum>(field, orderBy);
        }));
}
