using System.Linq.Expressions;
using DN.WebApi.Domain.Common.Contracts;
using Microsoft.EntityFrameworkCore.Query;

namespace DN.WebApi.Application.Common.Specifications;

public interface ISpecification<T>
where T : BaseEntity
{
    /*Expression<Func<T, bool>>? Criteria { get; }
    List<Expression<Func<T, object>>> Includes { get; }
    List<string> IncludeStrings { get; }

    Expression<Func<T, bool>> And(Expression<Func<T, bool>> query);

    Expression<Func<T, bool>> Or(Expression<Func<T, bool>> query);*/

    List<Expression<Func<T, bool>>> Conditions { get; set; }

    Func<IQueryable<T>, IIncludableQueryable<T, object>> Includes { get; set; }

    // List<string> IncludeStrings { get; }

    Func<IQueryable<T>, IOrderedQueryable<T>> OrderBy { get; set; }

    string[]? OrderByStrings { get; set; }
}