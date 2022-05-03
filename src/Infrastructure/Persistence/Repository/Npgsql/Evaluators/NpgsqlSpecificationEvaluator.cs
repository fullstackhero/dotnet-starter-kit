using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;

namespace FSH.WebApi.Infrastructure.Persistence.Repository.Npgsql.Evaluators;

public sealed class NpgsqlSpecificationEvaluator : SpecificationEvaluator
{
    public static NpgsqlSpecificationEvaluator Instance { get; } = new NpgsqlSpecificationEvaluator();

    private NpgsqlSpecificationEvaluator()
        : base(new IEvaluator[]
        {
            WhereEvaluator.Instance,
            NpgsqlSearchEvaluator.Instance,
            IncludeEvaluator.Default,
            OrderEvaluator.Instance,
            PaginationEvaluator.Instance,
            AsNoTrackingEvaluator.Instance,
            IgnoreQueryFiltersEvaluator.Instance,
            AsSplitQueryEvaluator.Instance,
            AsNoTrackingWithIdentityResolutionEvaluator.Instance
        })
    {
    }
}