using FSH.Framework.Eventing.Abstractions;

namespace FSH.Modules.Multitenancy.Contracts.Events;

/// <summary>
/// Raised when a tenant is created and subscribed to a billing plan. The Billing module reacts by
/// creating the active subscription and issuing the term's subscription invoice.
/// </summary>
public sealed record TenantSubscribedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string? TenantId,
    string CorrelationId,
    string Source,
    Guid PlanId,
    string PlanKey,
    DateTime PeriodStartUtc,
    DateTime PeriodEndUtc)
    : IIntegrationEvent;
