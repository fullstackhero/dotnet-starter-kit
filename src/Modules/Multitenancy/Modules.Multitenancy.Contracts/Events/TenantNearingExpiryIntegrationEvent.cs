using FSH.Framework.Eventing.Abstractions;

namespace FSH.Modules.Multitenancy.Contracts.Events;

/// <summary>
/// Raised by the daily expiry scan when an active tenant is within the configured lead time of its
/// <c>ValidUpto</c> (but not yet lapsed). Consumers notify the tenant so they can renew in time.
/// </summary>
public sealed record TenantNearingExpiryIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string? TenantId,
    string CorrelationId,
    string Source,
    string TenantName,
    string AdminEmail,
    string? PlanKey,
    DateTime ValidUpto,
    DateTime GraceEndsUtc,
    int DaysRemaining)
    : IIntegrationEvent;
