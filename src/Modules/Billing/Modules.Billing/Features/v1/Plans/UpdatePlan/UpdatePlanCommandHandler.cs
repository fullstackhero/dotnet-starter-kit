using FSH.Framework.Core.Exceptions;
using FSH.Modules.Billing.Contracts.v1.Plans;
using FSH.Modules.Billing.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Billing.Features.v1.Plans.UpdatePlan;

public sealed class UpdatePlanCommandHandler(BillingDbContext dbContext)
    : ICommandHandler<UpdatePlanCommand, Guid>
{
    public async ValueTask<Guid> Handle(UpdatePlanCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var plan = await dbContext.Plans.FirstOrDefaultAsync(p => p.Id == command.PlanId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Plan {command.PlanId} not found.");

        plan.Update(command.Name, command.MonthlyBasePrice, command.OverageRates, command.Interval, command.AnnualPrice);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return plan.Id;
    }
}
