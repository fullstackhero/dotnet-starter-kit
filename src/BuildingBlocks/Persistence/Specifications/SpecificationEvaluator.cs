using Microsoft.EntityFrameworkCore;

namespace FSH.Framework.Persistence;

/// <summary>
/// Internal evaluator that turns specifications into executable <see cref="IQueryable{T}"/> queries.
/// </summary>
internal static class SpecificationEvaluator
{
    /// <summary>
    /// Evaluates a specification against an input query to produce a configured IQueryable.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="inputQuery">The base queryable to apply the specification to.</param>
    /// <param name="specification">The specification containing query configuration.</param>
    /// <returns>A configured queryable with all specification rules applied.</returns>
    /// <exception cref="ArgumentNullException">Thrown when inputQuery or specification is null.</exception>
    public static IQueryable<T> GetQuery<T>(
        IQueryable<T> inputQuery,
        ISpecification<T> specification)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(inputQuery);
        ArgumentNullException.ThrowIfNull(specification);

        IQueryable<T> query = inputQuery;

        query = ApplyQueryBehaviors(query, specification);
        query = ApplyCriteria(query, specification);
        query = ApplyIncludes(query, specification);
        query = ApplyOrdering(query, specification);

        return query;
    }

    /// <summary>
    /// Evaluates a specification with projection against an input query.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TResult">The projected result type.</typeparam>
    /// <param name="inputQuery">The base queryable to apply the specification to.</param>
    /// <param name="specification">The specification containing query configuration and projection.</param>
    /// <returns>A configured queryable with specification rules and projection applied.</returns>
    /// <exception cref="ArgumentNullException">Thrown when inputQuery or specification is null.</exception>
    public static IQueryable<TResult> GetQuery<T, TResult>(
        IQueryable<T> inputQuery,
        ISpecification<T, TResult> specification)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(inputQuery);
        ArgumentNullException.ThrowIfNull(specification);

        var query = GetQuery(inputQuery, (ISpecification<T>)specification);

        // When a selector is configured, includes may be ignored at the EF level,
        // but behavior is consistently applied by always projecting at the end.
        return query.Select(specification.Selector);
    }

    private static IQueryable<T> ApplyQueryBehaviors<T>(IQueryable<T> query, ISpecification<T> specification)
        where T : class
    {
        if (specification.IgnoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }

        if (specification.AsNoTracking)
        {
            query = query.AsNoTracking();
        }

        if (specification.AsSplitQuery)
        {
            query = query.AsSplitQuery();
        }

        return query;
    }

    private static IQueryable<T> ApplyCriteria<T>(IQueryable<T> query, ISpecification<T> specification)
        where T : class
    {
        if (specification.Criteria is not null)
        {
            query = query.Where(specification.Criteria);
        }

        return query;
    }

    private static IQueryable<T> ApplyIncludes<T>(IQueryable<T> query, ISpecification<T> specification)
        where T : class
    {
        foreach (var include in specification.Includes)
        {
            query = query.Include(include);
        }

        foreach (var includeString in specification.IncludeStrings)
        {
            query = query.Include(includeString);
        }

        return query;
    }

    private static IQueryable<T> ApplyOrdering<T>(IQueryable<T> query, ISpecification<T> specification)
        where T : class
    {
        if (specification.OrderExpressions.Count == 0)
        {
            return query;
        }

        IOrderedQueryable<T>? ordered = null;

        foreach (var order in specification.OrderExpressions)
        {
            ordered = ApplyOrderExpression(query, ordered, order);
        }

        return ordered ?? query;
    }

    private static IOrderedQueryable<T> ApplyOrderExpression<T>(
        IQueryable<T> query,
        IOrderedQueryable<T>? ordered,
        OrderExpression<T> order)
        where T : class
    {
        if (ordered is null)
        {
            return order.Descending
                ? query.OrderByDescending(order.KeySelector)
                : query.OrderBy(order.KeySelector);
        }

        return order.Descending
            ? ordered.ThenByDescending(order.KeySelector)
            : ordered.ThenBy(order.KeySelector);
    }
}
