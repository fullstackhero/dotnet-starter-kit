using Ardalis.Specification;
using FSH.WebApi.Infrastructure.Persistence.Repository.Npgsql.Extensions;

namespace FSH.WebApi.Infrastructure.Persistence.Repository.Npgsql.Evaluators;
public sealed class NpgsqlSearchEvaluator : IEvaluator
{
    private NpgsqlSearchEvaluator()
    {
    }

    public static NpgsqlSearchEvaluator Instance { get; } = new NpgsqlSearchEvaluator();

    public bool IsCriteriaEvaluator { get; } = true;

    public IQueryable<T> GetQuery<T>(IQueryable<T> query, ISpecification<T> specification)
        where T : class
    {
        foreach (var searchCriteria in specification.SearchCriterias.GroupBy(x => x.SearchGroup))
        {
            query = query.Search(searchCriteria);
        }

        return query;
    }
}