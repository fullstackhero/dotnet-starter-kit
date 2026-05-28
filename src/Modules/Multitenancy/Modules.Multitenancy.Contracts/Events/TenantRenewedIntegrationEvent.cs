using FSH.Framework.Eventing.Abstractions;

namespace FSH.Modules.Multitenancy.Contracts.Events;

/// <summary>
/// Raised when a tenant is renewed (and possibly switched to a different plan). The Billing module
/// reacts by swapping the subscription when the plan changed and issuing the new term's invoice.
/// </summary>
public sealed record TenantRenewedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string? TenantId,
    string CorrelationId,
    string Source,
    Guid PlanId,
    string PlanKey,
    DateTime PeriodStartUtc,
    DateTime PeriodEndUtc,
    bool PlanChanged)
    : IIntegrationEvent;
