using System.Linq.Expressions;

namespace FSH.WebApi.Infrastructure.Persistence.Repository.Npgsql.Extensions;

internal sealed class ParameterReplacerVisitor : ExpressionVisitor
{
    private readonly Expression _newExpression;
    private readonly ParameterExpression _oldParameter;

    private ParameterReplacerVisitor(ParameterExpression oldParameter, Expression newExpression)
    {
        _oldParameter = oldParameter;
        _newExpression = newExpression;
    }

    internal static Expression Replace(Expression expression, ParameterExpression oldParameter, Expression newExpression)
    {
        return new ParameterReplacerVisitor(oldParameter, newExpression).Visit(expression);
    }

    protected override Expression VisitParameter(ParameterExpression p)
    {
        if (p == _oldParameter)
        {
            return _newExpression;
        }
        else
        {
            return p;
        }
    }
}