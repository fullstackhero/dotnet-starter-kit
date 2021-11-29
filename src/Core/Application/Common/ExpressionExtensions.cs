using System.Linq.Expressions;

namespace DN.WebApi.Application.Common;

public static class PredicateBuilder
{
    public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        ParameterExpression p = left.Parameters[0];
        SubstExpressionVisitor visitor = new SubstExpressionVisitor
        {
            Subst = { [right.Parameters[0]] = p }
        };

        Expression body = Expression.AndAlso(left.Body, visitor.Visit(right.Body));
        return Expression.Lambda<Func<T, bool>>(body, p);
    }

    public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        ParameterExpression p = left.Parameters[0];
        SubstExpressionVisitor visitor = new SubstExpressionVisitor
        {
            Subst = { [right.Parameters[0]] = p }
        };

        Expression body = Expression.OrElse(left.Body, visitor.Visit(right.Body));
        return Expression.Lambda<Func<T, bool>>(body, p);
    }
}

internal class SubstExpressionVisitor : ExpressionVisitor
{
    public Dictionary<Expression, Expression> Subst = new();

    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (Subst.TryGetValue(node, out var newValue))
        {
            return newValue;
        }

        return node;
    }
}