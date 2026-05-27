using System.Linq.Expressions;

namespace FSH.Framework.Persistence;

/// <summary>
/// Projected specification that composes a query for <typeparamref name="T"/>
/// and then selects into <typeparamref name="TResult"/>.
/// </summary>
/// <remarks>
/// Includes may be ignored when a selector is present; behavior is documented
/// at the evaluator/extension level.
/// </remarks>
/// <typeparam name="T">The root entity type.</typeparam>
/// <typeparam name="TResult">The projected result type.</typeparam>
public interface ISpecification<T, TResult> : ISpecification<T>
    where T : class
{
    /// <summary>
    /// Projection applied at the end of the query pipeline.
    /// </summary>
    Expression<Func<T, TResult>> Selector { get; }
}