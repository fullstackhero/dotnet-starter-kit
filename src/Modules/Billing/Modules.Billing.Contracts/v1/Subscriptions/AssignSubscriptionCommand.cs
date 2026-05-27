using Mediator;

namespace FSH.Modules.Billing.Contracts.v1.Subscriptions;

/// <summary>
/// Admin command to assign a tenant to a plan, starting now. If the tenant has an existing active
/// subscription it will be cancelled at this moment and replaced.
/// </summary>
public sealed record AssignSubscriptionCommand(string TenantId, string PlanKey) : ICommand<Guid>;
