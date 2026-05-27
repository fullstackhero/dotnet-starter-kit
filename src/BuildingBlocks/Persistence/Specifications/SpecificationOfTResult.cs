using FSH.Framework.Persistence.Specifications;
using System.Linq.Expressions;

namespace FSH.Framework.Persistence;

/// <summary>
/// Base specification that composes a query for <typeparamref name="T"/> and
/// projects it into <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="T">The root entity type.</typeparam>
/// <typeparam name="TResult">The projected result type.</typeparam>
public abstract class Specification<T, TResult> : Specification<T>, ISpecification<T, TResult>
    where T : class
{
    public Expression<Func<T, TResult>> Selector { get; private set; } = default!;

    /// <summary>
    /// Configures the projection applied at the end of the query pipeline.
    /// </summary>
    /// <param name="selector">Projection expression.</param>
    protected void Select(Expression<Func<T, TResult>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        Selector = selector;
    }
}