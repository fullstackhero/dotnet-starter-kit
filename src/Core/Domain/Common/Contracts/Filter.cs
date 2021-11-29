using System.Linq.Expressions;

namespace DN.WebApi.Domain.Common.Contracts;

public class Filter<T>
{
    public Filter(bool condition, Expression<Func<T, bool>> expression) =>
        (Condition, Expression) = (condition, expression);

    public bool Condition { get; }
    public Expression<Func<T, bool>> Expression { get; }
}