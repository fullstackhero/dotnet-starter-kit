using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace FSH.WebApi.Application.Common.Specification;

// See https://github.com/ardalis/Specification/issues/53
public static class SpecificationBuilderExtensions
{
    public static ISpecificationBuilder<T> SearchBy<T>(this ISpecificationBuilder<T> query, BaseFilter filter) =>
        query
            .SearchByKeyword(filter.Keyword)
            .AdvancedSearch(filter.AdvancedSearch)
            .AdvancedFilter(filter.AdvancedFilter);

    public static ISpecificationBuilder<T> PaginateBy<T>(this ISpecificationBuilder<T> query, PaginationFilter filter)
    {
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
            query = query.Skip((filter.PageNumber - 1) * filter.PageSize);
        }

        return query
            .Take(filter.PageSize)
            .OrderBy(filter.OrderBy);
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
                    MemberExpression propertyExpr = GetMemberExpression(field, paramExpr);

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

    private static MemberExpression GetMemberExpression(string propertyName, ParameterExpression parameter)
    {
        Expression mapProperty = parameter;
        foreach (string member in propertyName.Split('.'))
        {
            mapProperty = Expression.PropertyOrField(mapProperty, member);
        }

        return (MemberExpression)mapProperty;
    }

    private static BinaryExpression GetBinaryExpression(MemberExpression memberExpression, ConstantExpression constantExpression, ConstantExpression constantExpressionAux, Filter filter)
    {
        return filter.Operator switch
        {
            FilterOperator.EQ => Expression.Equal(memberExpression, constantExpression),
            FilterOperator.NEQ => Expression.NotEqual(memberExpression, constantExpression),
            FilterOperator.LT => Expression.LessThan(memberExpression, constantExpression),
            FilterOperator.LTE => Expression.LessThanOrEqual(memberExpression, constantExpression),
            FilterOperator.GT => Expression.GreaterThan(memberExpression, constantExpression),
            FilterOperator.GTE => Expression.GreaterThanOrEqual(memberExpression, constantExpression),
            FilterOperator.BETWEEN => Expression.AndAlso(Expression.GreaterThanOrEqual(memberExpression, constantExpression), Expression.LessThanOrEqual(memberExpression, constantExpressionAux)),
            _ => throw new ArgumentException("operatorSearch is not valid.", nameof(filter.Operator)),
        };
    }

    private static string GetStringFromJsonElement(object value) => ((JsonElement)value).GetString()!;

    private static BinaryExpression GetBinaryExpressionFromFilter(Filter filter, ParameterExpression parameter)
    {
        MemberExpression mapProperty = GetMemberExpression(filter.Field, parameter);

        (ConstantExpression value, ConstantExpression valueAx) = GetConstantExpressionFromFilter(mapProperty, filter);

        var bExpresion = GetBinaryExpression(mapProperty, value, valueAx, filter);
        if (filter.Filters.Any())
        {
            bExpresion = AddFilters(filter.Filters, parameter, bExpresion);
        }

        return bExpresion;
    }

    private static (ConstantExpression cExpresion, ConstantExpression cExpresionAux) GetConstantExpressionFromFilter(MemberExpression mapProperty, Filter filter)
    {
        ConstantExpression cExpresionAux = default!;

        ConstantExpression cExpresion;
        if (mapProperty.Type.IsEnum)
        {
            string? stringEnum = GetStringFromJsonElement(filter.Value!);

            if (!Enum.TryParse(mapProperty.Type, stringEnum, true, out object? valueparsed)) throw new CustomException("Value {0} is not valid for {1}");

            cExpresion = Expression.Constant(valueparsed, mapProperty.Type);
        }
        else if (mapProperty.Type == typeof(Guid))
        {
            string? stringGuid = GetStringFromJsonElement(filter.Value!);

            if (!Guid.TryParse(stringGuid, out Guid valueparsed)) throw new CustomException("Value {0} is not valid for {1}");

            cExpresion = Expression.Constant(valueparsed, mapProperty.Type);
        }
        else if (mapProperty.Type == typeof(string))
        {
            string? text = GetStringFromJsonElement(filter.Value!);

            cExpresion = Expression.Constant(text, mapProperty.Type);
        }
        else
        {
            cExpresion = Expression.Constant(Convert.ChangeType(((JsonElement)filter.Value!).GetRawText(), mapProperty.Type), mapProperty.Type);
            if (filter.ValueAux is not null)
            {
                cExpresionAux = Expression.Constant(Convert.ChangeType(((JsonElement)filter.ValueAux).GetRawText(), mapProperty.Type), mapProperty.Type);
            }
        }

        return (cExpresion, cExpresionAux);
    }

    private static BinaryExpression AddFilters(IEnumerable<Filter> filters, ParameterExpression parameter, BinaryExpression bExpresionBase = default!)
    {
        BinaryExpression bResult = default!;
        foreach (Filter filter in filters)
        {
            var bExpresionFilter = GetBinaryExpressionFromFilter(filter, parameter);

            if (bExpresionBase is not null)
            {
                bResult = filter.Logic switch
                {
                    FilterLogic.AND => Expression.And(bExpresionBase, bExpresionFilter),
                    FilterLogic.OR => Expression.Or(bExpresionBase, bExpresionFilter),
                    FilterLogic.XOR => Expression.ExclusiveOr(bExpresionBase, bExpresionFilter),
                    _ => throw new ArgumentException("FilterLogic is not valid.", nameof(FilterLogic)),
                };
            }
            else
            {
                bResult = bExpresionFilter;
            }
        }

        return bResult;
    }

    public static IOrderedSpecificationBuilder<T> AdvancedFilter<T>(
        this ISpecificationBuilder<T> specificationBuilder,
        IEnumerable<Filter>? filters)
    {
        if (filters is not null)
        {
            List<string> operatorsSearch = new List<string>
            {
                FilterOperator.STARTSWITH,
                FilterOperator.ENDSWITH,
                FilterOperator.CONTAINS
            };

            // search seleted fields (can contain deeper nested fields)
            foreach (Filter filter in filters)
            {
                var parameter = Expression.Parameter(typeof(T));

                // TODO: Add support for nested filter: like
                if (operatorsSearch.Contains(filter.Operator))
                {
                    specificationBuilder.AddSearchPropertyByKeyword(GetMemberExpression(filter.Field, parameter), parameter, GetStringFromJsonElement(filter.Value), filter.Operator);
                    continue;
                }

                var binaryExpresioFilter = GetBinaryExpressionFromFilter(filter, parameter);

                ((List<WhereExpressionInfo<T>>)specificationBuilder.Specification.WhereExpressions)
                    .Add(new WhereExpressionInfo<T>(Expression.Lambda<Func<T, bool>>(binaryExpresioFilter, parameter)));
            }
        }

        return new OrderedSpecificationBuilder<T>(specificationBuilder.Specification);
    }

    private static void AddSearchPropertyByKeyword<T>(this ISpecificationBuilder<T> specificationBuilder, Expression propertyExpr, ParameterExpression paramExpr, string keyword, string operatorSearch = FilterOperator.CONTAINS)
    {
        if (propertyExpr is not MemberExpression memberExpr || memberExpr.Member is not PropertyInfo property)
        {
            throw new ArgumentException("propertyExpr must be a property expression.", nameof(propertyExpr));
        }

        string searchTerm = operatorSearch switch
        {
            FilterOperator.STARTSWITH => $"{keyword}%",
            FilterOperator.ENDSWITH => $"%{keyword}",
            FilterOperator.CONTAINS => $"%{keyword}%",
            _ => throw new ArgumentException("operatorSearch is not valid.", nameof(operatorSearch))
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

        var selector = Expression.Lambda<Func<T, string>>(selectorExpr, paramExpr);

        ((List<SearchExpressionInfo<T>>)specificationBuilder.Specification.SearchCriterias)
            .Add(new SearchExpressionInfo<T>(selector, searchTerm, 1));
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