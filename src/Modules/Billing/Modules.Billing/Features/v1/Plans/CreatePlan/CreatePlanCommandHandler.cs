using FSH.Modules.Billing.Contracts.v1.Plans;
using FSH.Modules.Billing.Data;
using FSH.Modules.Billing.Domain;
using Mediator;

namespace FSH.Modules.Billing.Features.v1.Plans.CreatePlan;

public sealed class CreatePlanCommandHandler(BillingDbContext dbContext)
    : ICommandHandler<CreatePlanCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreatePlanCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var plan = BillingPlan.Create(command.Key, command.Name, command.Currency, command.MonthlyBasePrice, command.OverageRates);
        dbContext.Plans.Add(plan);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return plan.Id;
    }
}
