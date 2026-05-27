using System.Linq.Expressions;

namespace FSH.Framework.Persistence;

/// <summary>
/// Normalized representation of an ordering expression for specifications.
/// </summary>
/// <typeparam name="T">The root entity type.</typeparam>
public sealed record OrderExpression<T>(
    Expression<Func<T, object>> KeySelector,
    bool Descending)
    where T : class;