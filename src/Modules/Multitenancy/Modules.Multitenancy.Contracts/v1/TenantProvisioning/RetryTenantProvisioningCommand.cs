using FSH.Modules.Multitenancy.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Multitenancy.Contracts.v1.TenantProvisioning;

public sealed record RetryTenantProvisioningCommand(string TenantId) : ICommand<TenantProvisioningStatusDto>;
