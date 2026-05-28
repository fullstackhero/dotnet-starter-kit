using Mediator;

namespace FSH.Modules.Billing.Contracts.v1.Plans;

/// <summary>
/// Reads a plan's billing term so another module (Multitenancy) can compute a tenant's validity
/// window without referencing Billing's runtime. Dispatched via Mediator across the module boundary.
/// </summary>
public sealed record GetPlanTermQuery(string PlanKey) : IQuery<PlanTermResponse>;

public sealed record PlanTermResponse(
    Guid PlanId,
    string Key,
    string Name,
    PlanInterval Interval,
    int TermMonths,
    decimal UnitPrice,
    string Currency);
