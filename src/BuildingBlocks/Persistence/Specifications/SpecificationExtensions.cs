namespace FSH.Framework.Persistence;

/// <summary>
/// Extension methods to apply specifications to <see cref="IQueryable{T}"/> instances.
/// </summary>
public static class SpecificationExtensions
{
    /// <summary>
    /// Applies an entity-level specification to the query.
    /// </summary>
    public static IQueryable<T> ApplySpecification<T>(
        this IQueryable<T> query,
        ISpecification<T> specification)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(query);
        return SpecificationEvaluator.GetQuery(query, specification);
    }

    /// <summary>
    /// Applies a projected specification to the query.
    /// </summary>
    public static IQueryable<TResult> ApplySpecification<T, TResult>(
        this IQueryable<T> query,
        ISpecification<T, TResult> specification)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(query);
        return SpecificationEvaluator.GetQuery(query, specification);
    }
}