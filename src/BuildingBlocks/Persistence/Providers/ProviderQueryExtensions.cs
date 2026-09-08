using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace FSH.Framework.Persistence.Providers;

/// <summary>
/// Query helpers that paper over translation differences between the supported providers.
/// </summary>
public static class ProviderQueryExtensions
{
    private static readonly MethodInfo LikeMethod = typeof(DbFunctionsExtensions).GetMethod(
        nameof(DbFunctionsExtensions.Like),
        BindingFlags.Public | BindingFlags.Static,
        [typeof(DbFunctions), typeof(string), typeof(string)])!;

    private static readonly MethodInfo ILikeMethod = typeof(NpgsqlDbFunctionsExtensions).GetMethod(
        nameof(NpgsqlDbFunctionsExtensions.ILike),
        BindingFlags.Public | BindingFlags.Static,
        [typeof(DbFunctions), typeof(string), typeof(string)])!;

    /// <summary>
    /// Filters to rows where any of <paramref name="selectors"/> contains <paramref name="term"/>,
    /// case-insensitively, using whichever construct the current provider translates.
    /// </summary>
    /// <remarks>
    /// <para>
    /// PostgreSQL gets <c>ILIKE</c> — the same SQL the handlers emitted before this helper existed,
    /// so the trigram GIN indexes still apply. Every other provider gets <c>LIKE</c>, which is
    /// case-insensitive on SQL Server under its default collation.
    /// </para>
    /// <para>
    /// The term is not escaped, preserving the pre-existing behaviour where <c>%</c> and <c>_</c> in
    /// a search box act as wildcards. Callers that need literal matching must escape up front.
    /// </para>
    /// </remarks>
    /// <param name="source">The query to filter.</param>
    /// <param name="database">The context's database facade, used to detect the provider.</param>
    /// <param name="term">The substring to search for.</param>
    /// <param name="selectors">The columns to search. Null values never match.</param>
    public static IQueryable<TEntity> WhereSearch<TEntity>(
        this IQueryable<TEntity> source,
        DatabaseFacade database,
        string term,
        params Expression<Func<TEntity, string?>>[] selectors)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(selectors);

        if (selectors.Length == 0)
        {
            return source;
        }

        return source.WhereLikeCore(database, $"%{term}%", selectors);
    }

    /// <summary>
    /// Filters to rows where <paramref name="selector"/> matches a caller-supplied LIKE pattern,
    /// case-insensitively, using whichever construct the current provider translates.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="WhereSearch"/> the pattern is used verbatim, so the caller controls the
    /// wildcards. Intended for structural matches such as
    /// <see cref="JsonTextPropertyPattern"/>, not for user-typed search boxes.
    /// </remarks>
    public static IQueryable<TEntity> WhereLike<TEntity>(
        this IQueryable<TEntity> source,
        DatabaseFacade database,
        string pattern,
        Expression<Func<TEntity, string?>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return source.WhereLikeCore(database, pattern, [selector]);
    }

    /// <summary>
    /// Builds a LIKE pattern matching a <c>"name": "value"</c> pair inside a JSON document that has
    /// been cast to text.
    /// </summary>
    /// <remarks>
    /// The two providers render JSON to text differently and the difference is load-bearing here:
    /// PostgreSQL's <c>jsonb::text</c> emits canonical form <em>with</em> a space after the colon
    /// (<c>{"area": "Value"}</c>), while SQL Server's native <c>json</c> type casts to compact form
    /// <em>without</em> one (<c>{"area":"Value"}</c>). A pattern hard-coded for either provider
    /// silently matches nothing on the other.
    /// </remarks>
    /// <param name="database">The context's database facade, used to detect the provider.</param>
    /// <param name="propertyName">The JSON property name.</param>
    /// <param name="value">The value to match.</param>
    /// <param name="exact">
    /// When true the value must terminate (closing quote included); when false the pattern matches
    /// any value starting with <paramref name="value"/>.
    /// </param>
    public static string JsonTextPropertyPattern(
        DatabaseFacade database,
        string propertyName,
        string value,
        bool exact = true)
    {
        ArgumentNullException.ThrowIfNull(database);

        string separator = database.IsNpgsql() ? ": " : ":";
        string suffix = exact ? "\"%" : "%";
        return $"%\"{propertyName}\"{separator}\"{value}{suffix}";
    }

    private static IQueryable<TEntity> WhereLikeCore<TEntity>(
        this IQueryable<TEntity> source,
        DatabaseFacade database,
        string pattern,
        Expression<Func<TEntity, string?>>[] selectors)
    {
        MethodInfo likeMethod = database.IsNpgsql() ? ILikeMethod : LikeMethod;
        ConstantExpression functions = Expression.Constant(EF.Functions);
        ConstantExpression patternExpression = Expression.Constant(pattern, typeof(string));

        ParameterExpression entity = Expression.Parameter(typeof(TEntity), "e");
        Expression? predicate = null;

        foreach (Expression<Func<TEntity, string?>> selector in selectors)
        {
            Expression column = ParameterRebinder.Rebind(selector.Body, selector.Parameters[0], entity);

            // Mirrors the null guards the handlers wrote by hand. Redundant in SQL (LIKE on NULL is
            // NULL, which filters the row out anyway) but kept so the emitted SQL is unchanged.
            Expression clause = Expression.AndAlso(
                Expression.NotEqual(column, Expression.Constant(null, typeof(string))),
                Expression.Call(likeMethod, functions, column, patternExpression));

            predicate = predicate is null ? clause : Expression.OrElse(predicate, clause);
        }

        return source.Where(Expression.Lambda<Func<TEntity, bool>>(predicate!, entity));
    }

    private sealed class ParameterRebinder(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        public static Expression Rebind(Expression body, ParameterExpression from, ParameterExpression to)
            => new ParameterRebinder(from, to).Visit(body);

        protected override Expression VisitParameter(ParameterExpression node)
            => node == from ? to : base.VisitParameter(node);
    }
}
