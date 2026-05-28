using Mediator;

namespace FSH.Modules.Multitenancy.Contracts.v1.RenewTenant;

/// <summary>
/// Renews a tenant for one more plan term. When <see cref="PlanKey"/> is null the current plan is
/// renewed; when it differs the tenant is switched to the new plan from the renewal forward.
/// </summary>
public sealed record RenewTenantCommand(string TenantId, string? PlanKey = null)
    : ICommand<RenewTenantCommandResponse>;
