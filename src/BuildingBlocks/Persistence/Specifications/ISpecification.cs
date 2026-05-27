using System.Linq.Expressions;

namespace FSH.Framework.Persistence;

/// <summary>
/// Entity-level specification describing how to compose a query for <typeparamref name="T"/>.
/// Specifications are responsible only for query composition – never pagination.
/// </summary>
/// <typeparam name="T">The root entity type.</typeparam>
public interface ISpecification<T>
    where T : class
{
    /// <summary>
    /// Optional filter criteria applied via <see cref="Queryable.Where{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}})"/>.
    /// </summary>
    Expression<Func<T, bool>>? Criteria { get; }

    /// <summary>
    /// Strongly-typed include expressions.
    /// </summary>
    IReadOnlyList<Expression<Func<T, object>>> Includes { get; }

    /// <summary>
    /// String-based include paths.
    /// </summary>
    IReadOnlyList<string> IncludeStrings { get; }

    /// <summary>
    /// Default ordering expressions applied when no client-side sort override is present.
    /// </summary>
    IReadOnlyList<OrderExpression<T>> OrderExpressions { get; }

    /// <summary>
    /// When true (default), queries are executed with <c>AsNoTracking()</c>.
    /// </summary>
    bool AsNoTracking { get; }

    /// <summary>
    /// When true, queries are executed with <c>AsSplitQuery()</c>.
    /// </summary>
    bool AsSplitQuery { get; }

    /// <summary>
    /// When true, EF Core global query filters are ignored.
    /// </summary>
    bool IgnoreQueryFilters { get; }
}