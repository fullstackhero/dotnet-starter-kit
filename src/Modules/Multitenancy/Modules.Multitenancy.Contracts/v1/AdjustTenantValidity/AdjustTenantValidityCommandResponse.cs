namespace FSH.Modules.Multitenancy.Contracts.v1.AdjustTenantValidity;

public sealed record AdjustTenantValidityCommandResponse(string TenantId, DateTime ValidUpto);
