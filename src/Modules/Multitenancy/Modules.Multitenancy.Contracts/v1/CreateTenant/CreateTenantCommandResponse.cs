namespace FSH.Modules.Multitenancy.Contracts.v1.CreateTenant;

public sealed record CreateTenantCommandResponse(
    string Id,
    string ProvisioningCorrelationId,
    string Status);
