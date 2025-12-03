using FSH.Modules.Multitenancy.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Multitenancy.Contracts.v1.TenantProvisioning;

public sealed record GetTenantProvisioningStatusQuery(string TenantId) : IQuery<TenantProvisioningStatusDto>;
