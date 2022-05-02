using System.Reflection;
using Ardalis.Specification;
using FSH.WebApi.Infrastructure.Persistence.Specification.Extensions;

namespace FSH.WebApi.Infrastructure.Persistence.Specification.Evaluators;
public sealed class FSHSearchEvaluator : IEvaluator
{
    private MethodInfo SearchMethod { get; }

    private FSHSearchEvaluator(bool isNpgsql = false)
    {
        SearchMethod = isNpgsql ? FSHSearchExtension.ILikeMethodInfo : FSHSearchExtension.LikeMethodInfo;
    }

    public static FSHSearchEvaluator DefaultInstance { get; } = new FSHSearchEvaluator();
    public static FSHSearchEvaluator NpgsqlInstance { get; } = new FSHSearchEvaluator(true);

    public bool IsCriteriaEvaluator { get; } = true;

    public IQueryable<T> GetQuery<T>(IQueryable<T> query, ISpecification<T> specification)
        where T : class
    {
        foreach (var searchCriteria in specification.SearchCriterias.GroupBy(x => x.SearchGroup))
        {
            query = query.Search(searchCriteria, SearchMethod);
        }

        return query;
    }
}