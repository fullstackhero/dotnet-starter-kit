using DN.WebApi.Shared.DTOs.Filters;
using System.Linq.Expressions;

namespace DN.WebApi.Infrastructure.Persistence.Repositories;

public static class PredicateBuilder
{
    public static Expression<Func<T, bool>> True<T>()
    {
        return _ => true;
    }

    public static Expression<Func<T, bool>> False<T>()
    {
        return _ => false;
    }

    public static IQueryable<T> AdvancedSearch<T>(this IQueryable<T> query, Search search)
    {
        var predicate = False<T>();
        foreach (var propertyInfo in typeof(T).GetProperties().Where(p => search.Fields.Any(field => p.Name.ToLower() == field.ToLower())))
        {
            if (propertyInfo.GetGetMethod().IsVirtual)
                continue;

            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, propertyInfo);
            var propertyAsObject = Expression.Convert(property, typeof(object));
            var nullCheck = Expression.NotEqual(propertyAsObject, Expression.Constant(null, typeof(object)));
            var propertyAsString = Expression.Call(property, "ToString", null, null);
            var keywordExpression = Expression.Constant(search.Keyword);
            var contains = propertyInfo.PropertyType == typeof(string) ? Expression.Call(property, "Contains", null, keywordExpression) : Expression.Call(propertyAsString, "Contains", null, keywordExpression);
            var lambda = Expression.Lambda(Expression.AndAlso(nullCheck, contains), parameter);
            predicate = predicate.Or((Expression<Func<T, bool>>)lambda);
        }

        return query.Where(predicate);
    }

    public static IQueryable<T> SearchByKeyword<T>(this IQueryable<T> query, string keyword)
    {
        var predicate = False<T>();
        var properties = typeof(T).GetProperties();
        foreach (var propertyInfo in properties)
        {
            if (propertyInfo.GetGetMethod().IsVirtual)
                continue;

            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, propertyInfo);
            var propertyAsObject = Expression.Convert(property, typeof(object));
            var nullCheck = Expression.NotEqual(propertyAsObject, Expression.Constant(null, typeof(object)));
            var propertyAsString = Expression.Call(property, "ToString", null, null);
            var keywordExpression = Expression.Constant(keyword);
            var contains = propertyInfo.PropertyType == typeof(string) ? Expression.Call(property, "Contains", null, keywordExpression) : Expression.Call(propertyAsString, "Contains", null, keywordExpression);
            var lambda = Expression.Lambda(Expression.AndAlso(nullCheck, contains), parameter);
            predicate = predicate.Or((Expression<Func<T, bool>>)lambda);
        }

        return query.Where(predicate);
    }

    public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
    {
        var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>());
        return Expression.Lambda<Func<T, bool>>(Expression.OrElse(expr1.Body, invokedExpr), expr1.Parameters);
    }

    public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
    {
        var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>());
        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(expr1.Body, invokedExpr), expr1.Parameters);
    }
}