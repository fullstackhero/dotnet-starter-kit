using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;

namespace FSH.WebApi.Infrastructure.Persistence.Specification.Evaluators;

public sealed class FSHSpecificationEvaluator : SpecificationEvaluator
{
    public static FSHSpecificationEvaluator DefaultInstance { get; } = new FSHSpecificationEvaluator();
    public static FSHSpecificationEvaluator NpgsqlInstance { get; } = new FSHSpecificationEvaluator(true);

    private FSHSpecificationEvaluator(bool isNpgsql = false)
        : base(new IEvaluator[]
        {
            WhereEvaluator.Instance,
            isNpgsql ? FSHSearchEvaluator.NpgsqlInstance : FSHSearchEvaluator.DefaultInstance,
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