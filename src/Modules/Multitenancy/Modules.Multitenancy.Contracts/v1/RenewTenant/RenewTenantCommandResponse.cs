namespace FSH.Modules.Multitenancy.Contracts.v1.RenewTenant;

public sealed record RenewTenantCommandResponse(
    string TenantId,
    DateTime ValidUpto,
    string PlanKey,
    bool PlanChanged);
