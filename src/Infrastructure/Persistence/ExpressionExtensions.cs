using System.Linq.Expressions;

namespace DN.WebApi.Infrastructure.Persistence;

/// <summary>
///     Provides extension methods to the <see cref="Expression" /> class.
/// </summary>
public static class ExpressionExtensions
{
    /// <summary>
    ///     Converts the property accessor lambda expression to a textual representation of it's path. <br />
    ///     The textual representation consists of the properties that the expression access flattened and separated by a dot character (".").
    /// </summary>
    /// <param name="expression">The property selector expression.</param>
    /// <returns>The extracted textual representation of the expression's path.</returns>
    public static string AsPath(this LambdaExpression expression)
    {
        if (expression == null)
        {
            throw new ArgumentNullException(nameof(expression));
        }

        TryParsePath(expression.Body, out string path);

        return path;
    }

    /// <summary>
    ///     Recursively parses an expression tree representing a property accessor to extract a textual representation of it's path. <br />
    ///     The textual representation consists of the properties accessed by the expression tree flattened and separated by a dot character (".").
    /// </summary>
    /// <param name="expression">The expression tree to parse.</param>
    /// <param name="path">The extracted textual representation of the expression's path.</param>
    /// <returns>True if the parse operation succeeds; otherwise, false.</returns>
    private static bool TryParsePath(Expression expression, out string path)
    {
        var noConvertExp = RemoveConvertOperations(expression);
        path = default!;

        switch (noConvertExp)
        {
            case MemberExpression memberExpression:
                {
                    string currentPart = memberExpression.Member.Name;

                    if (!TryParsePath(memberExpression.Expression!, out string parentPart))
                        return false;

                    path = string.IsNullOrEmpty(parentPart) ? currentPart : string.Concat(parentPart, ".", currentPart);
                    break;
                }

            case MethodCallExpression callExpression:
                switch (callExpression.Method.Name)
                {
                    case nameof(Queryable.Select) when callExpression.Arguments.Count == 2:
                        {
                            if (!TryParsePath(callExpression.Arguments[0], out string parentPart))
                                return false;

                            if (string.IsNullOrEmpty(parentPart))
                                return false;

                            if (callExpression.Arguments[1] is not LambdaExpression subExpression)
                                return false;

                            if (!TryParsePath(subExpression.Body, out string currentPart))
                                return false;

                            if (string.IsNullOrEmpty(parentPart))
                                return false;

                            path = string.Concat(parentPart, ".", currentPart);
                            return true;
                        }

                    case nameof(Queryable.Where):
                        throw new NotSupportedException("Filtering an Include expression is not supported");
                    case nameof(Queryable.OrderBy):
                    case nameof(Queryable.OrderByDescending):
                        throw new NotSupportedException("Ordering an Include expression is not supported");
                    default:
                        return false;
                }
        }

        return true;
    }

    /// <summary>
    ///     Removes all casts or conversion operations from the nodes of the provided <see cref="Expression" />.
    ///     Used to prevent type boxing when manipulating expression trees.
    /// </summary>
    /// <param name="expression">The expression to remove the conversion operations.</param>
    /// <returns>The expression without conversion or cast operations.</returns>
    private static Expression RemoveConvertOperations(Expression expression)
    {
        while (expression.NodeType == ExpressionType.Convert || expression.NodeType == ExpressionType.ConvertChecked)
            expression = ((UnaryExpression)expression).Operand;

        return expression;
    }
}
