using FSH.Modules.Billing.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Billing.Contracts.v1.Subscriptions;

/// <summary>
/// Returns the current active subscription for the specified tenant. Tenant callers typically
/// pass null to read their own; admin callers can pass any tenant id.
/// </summary>
public sealed record GetSubscriptionQuery(string? TenantId = null) : IQuery<SubscriptionDto?>;
