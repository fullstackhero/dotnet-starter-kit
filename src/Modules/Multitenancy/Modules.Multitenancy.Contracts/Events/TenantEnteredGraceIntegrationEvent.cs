using FSH.Framework.Eventing.Abstractions;

namespace FSH.Modules.Multitenancy.Contracts.Events;

/// <summary>
/// Raised by the daily expiry scan when a tenant has passed <c>ValidUpto</c> but is still inside the
/// grace period (access continues). Consumers warn the tenant that the grace period is counting down.
/// </summary>
public sealed record TenantEnteredGraceIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string? TenantId,
    string CorrelationId,
    string Source,
    string TenantName,
    string AdminEmail,
    string? PlanKey,
    DateTime ValidUpto,
    DateTime GraceEndsUtc)
    : IIntegrationEvent;
