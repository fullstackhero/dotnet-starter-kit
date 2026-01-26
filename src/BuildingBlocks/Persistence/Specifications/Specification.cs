using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace FSH.Framework.Persistence.Specifications;

/// <summary>
/// Base specification for entity-level queries.
/// </summary>
/// <typeparam name="T">The root entity type.</typeparam>
public abstract class Specification<T> : ISpecification<T>
    where T : class
{
    private readonly List<Expression<Func<T, bool>>> _criteria = [];
    private readonly List<Expression<Func<T, object>>> _includes = [];
    private readonly List<string> _includeStrings = [];
    private readonly List<OrderExpression<T>> _orderExpressions = [];

    protected Specification()
    {
        // Favor read-only queries by default.
        AsNoTracking = true;
    }

    public Expression<Func<T, bool>>? Criteria =>
        _criteria.Count == 0
            ? null
            : _criteria.Aggregate((current, next) => Combine(current, next));

    public IReadOnlyList<Expression<Func<T, object>>> Includes =>
        new ReadOnlyCollection<Expression<Func<T, object>>>(_includes);

    public IReadOnlyList<string> IncludeStrings =>
        new ReadOnlyCollection<string>(_includeStrings);

    public IReadOnlyList<OrderExpression<T>> OrderExpressions =>
        new ReadOnlyCollection<OrderExpression<T>>(_orderExpressions);

    public bool AsNoTracking { get; private set; }

    public bool AsSplitQuery { get; private set; }

    public bool IgnoreQueryFilters { get; private set; }

    /// <summary>
    /// Adds a filter criteria expression combined with existing criteria via logical AND.
    /// </summary>
    /// <param name="expression">The filter expression.</param>
    protected void Where(Expression<Func<T, bool>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        _criteria.Add(expression);
    }

    /// <summary>
    /// Adds a strongly-typed include expression.
    /// </summary>
    protected void Include(Expression<Func<T, object>> includeExpression)
    {
        ArgumentNullException.ThrowIfNull(includeExpression);
        _includes.Add(includeExpression);
    }

    /// <summary>
    /// Adds a string-based include path.
    /// </summary>
    protected void Include(string includeString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(includeString);
        _includeStrings.Add(includeString);
    }

    /// <summary>
    /// Configures primary ascending ordering.
    /// </summary>
    protected void OrderBy(Expression<Func<T, object>> keySelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        _orderExpressions.Add(new OrderExpression<T>(keySelector, Descending: false));
    }

    /// <summary>
    /// Configures primary descending ordering.
    /// </summary>
    protected void OrderByDescending(Expression<Func<T, object>> keySelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        _orderExpressions.Add(new OrderExpression<T>(keySelector, Descending: true));
    }

    /// <summary>
    /// Appends ascending ordering (ThenBy).
    /// </summary>
    protected void ThenBy(Expression<Func<T, object>> keySelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        _orderExpressions.Add(new OrderExpression<T>(keySelector, Descending: false));
    }

    /// <summary>
    /// Appends descending ordering (ThenByDescending).
    /// </summary>
    protected void ThenByDescending(Expression<Func<T, object>> keySelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        _orderExpressions.Add(new OrderExpression<T>(keySelector, Descending: true));
    }

    /// <summary>
    /// Clears all configured ordering expressions.
    /// </summary>
    protected void ClearOrderExpressions() => _orderExpressions.Clear();

    /// <summary>
    /// Enables <c>AsNoTracking()</c> for the query (default).
    /// </summary>
    protected void AsNoTrackingQuery() => AsNoTracking = true;

    /// <summary>
    /// Enables tracking queries by disabling <c>AsNoTracking()</c>.
    /// </summary>
    protected void AsTrackingQuery() => AsNoTracking = false;

    /// <summary>
    /// Enables <c>AsSplitQuery()</c>.
    /// </summary>
    protected void AsSplitQueryBehavior() => AsSplitQuery = true;

    /// <summary>
    /// Enables <c>IgnoreQueryFilters()</c>.
    /// </summary>
    protected void IgnoreQueryFiltersBehavior() => IgnoreQueryFilters = true;

    /// <summary>
    /// Applies a client-provided sort expression using a whitelist of sort keys.
    /// When a non-empty sort expression is provided and at least one valid key is resolved,
    /// the client ordering overrides any existing specification ordering.
    /// When the sort expression is empty or contains only invalid keys, the provided
    /// <paramref name="applyDefaultOrdering"/> delegate is invoked to configure a
    /// deterministic default ordering.
    /// </summary>
    /// <param name="sortExpression">Multi-column sort expression, for example: "Name,-CreatedOn".</param>
    /// <param name="applyDefaultOrdering">
    /// Delegate that configures the default ordering using the specification's ordering helpers.
    /// </param>
    /// <param name="sortMappings">
    /// Whitelisted mapping from sort keys to strongly-typed expressions. No reflection is used.
    /// </param>
    protected void ApplySortingOverride(
        string? sortExpression,
        Action applyDefaultOrdering,
        IReadOnlyDictionary<string, Expression<Func<T, object>>> sortMappings)
    {
        ArgumentNullException.ThrowIfNull(applyDefaultOrdering);
        ArgumentNullException.ThrowIfNull(sortMappings);

        ClearOrderExpressions();

        if (string.IsNullOrWhiteSpace(sortExpression))
        {
            applyDefaultOrdering();
            return;
        }

        var clauses = ParseSortClauses(sortExpression);
        bool anyApplied = ApplySortClauses(clauses, sortMappings);

        if (!anyApplied)
        {
            applyDefaultOrdering();
        }
    }

    private static IEnumerable<string> ParseSortClauses(string sortExpression)
    {
        return sortExpression.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(clause => !string.IsNullOrWhiteSpace(clause));
    }

    private bool ApplySortClauses(IEnumerable<string> clauses, IReadOnlyDictionary<string, Expression<Func<T, object>>> sortMappings)
    {
        bool anyApplied = false;

        foreach (string rawClause in clauses)
        {
            var (key, descending) = ParseSortClause(rawClause);

            if (string.IsNullOrWhiteSpace(key) || !sortMappings.TryGetValue(key, out var selector))
            {
                continue;
            }

            ApplySortOrder(selector, descending, anyApplied);
            anyApplied = true;
        }

        return anyApplied;
    }

    private static (string key, bool descending) ParseSortClause(string clause)
    {
        clause = clause.Trim();
        bool descending = clause[0] == '-';
        string key = clause[0] is '-' or '+' ? clause[1..] : clause;
        return (key, descending);
    }

    private void ApplySortOrder(Expression<Func<T, object>> selector, bool descending, bool isSecondary)
    {
        if (isSecondary)
        {
            if (descending) ThenByDescending(selector);
            else ThenBy(selector);
        }
        else
        {
            if (descending) OrderByDescending(selector);
            else OrderBy(selector);
        }
    }

    private static Expression<Func<T, bool>> Combine(
        Expression<Func<T, bool>> first,
        Expression<Func<T, bool>> second)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var left = ReplaceParameter(first.Body, first.Parameters[0], parameter);
        var right = ReplaceParameter(second.Body, second.Parameters[0], parameter);
        var body = Expression.AndAlso(left, right);
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    private static Expression ReplaceParameter(
        Expression expression,
        ParameterExpression source,
        ParameterExpression target)
    {
        return new ParameterReplaceVisitor(source, target).Visit(expression)
               ?? throw new InvalidOperationException("Failed to replace parameter in expression.");
    }

    private sealed class ParameterReplaceVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _source;
        private readonly ParameterExpression _target;

        public ParameterReplaceVisitor(ParameterExpression source, ParameterExpression target)
        {
            _source = source;
            _target = target;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _source ? _target : base.VisitParameter(node);
        }
    }
}
