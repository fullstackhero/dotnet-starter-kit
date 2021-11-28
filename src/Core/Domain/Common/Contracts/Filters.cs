using System.Linq.Expressions;

namespace DN.WebApi.Domain.Common.Contracts;

public class Filters<T>
{
    private readonly List<Filter<T>> _filterList;

    public Filters()
    {
        _filterList = new List<Filter<T>>();
    }

    public void Add(bool condition, Expression<Func<T, bool>> expression)
    {
        _filterList.Add(new Filter<T>
        {
            Condition = condition,
            Expression = expression
        });
    }

    public bool IsValid()
    {
        return _filterList.Any(f => f.Condition);
    }

    public List<Filter<T>> Get()
    {
        return _filterList.Where(f => f.Condition).ToList();
    }
}