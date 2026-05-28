using FSH.Framework.Core.Exceptions;
using FSH.Modules.Billing.Contracts.v1.Plans;
using FSH.Modules.Billing.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Billing.Features.v1.Plans.GetPlanTerm;

public sealed class GetPlanTermQueryHandler(BillingDbContext dbContext)
    : IQueryHandler<GetPlanTermQuery, PlanTermResponse>
{
    public async ValueTask<PlanTermResponse> Handle(GetPlanTermQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

#pragma warning disable CA1308 // Plan keys are canonical lowercase slugs
        var key = query.PlanKey.ToLowerInvariant();
#pragma warning restore CA1308
        var plan = await dbContext.Plans.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Key == key && p.IsActive, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Active plan with key '{query.PlanKey}' not found.");

        return new PlanTermResponse(
            plan.Id,
            plan.Key,
            plan.Name,
            plan.Interval,
            plan.TermMonths,
            plan.TermPrice,
            plan.Currency);
    }
}
