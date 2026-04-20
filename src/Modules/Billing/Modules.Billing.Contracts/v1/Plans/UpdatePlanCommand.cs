using FSH.Framework.Shared.Quota;
using Mediator;

namespace FSH.Modules.Billing.Contracts.v1.Plans;

public sealed record UpdatePlanCommand(
    Guid PlanId,
    string Name,
    decimal MonthlyBasePrice,
    IReadOnlyDictionary<QuotaResource, decimal>? OverageRates = null) : ICommand<Guid>;
