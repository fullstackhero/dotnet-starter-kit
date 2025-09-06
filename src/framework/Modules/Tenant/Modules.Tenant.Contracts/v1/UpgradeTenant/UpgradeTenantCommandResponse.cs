namespace FSH.Framework.Tenant.Contracts.v1.UpgradeTenant;
public sealed record UpgradeTenantCommandResponse(DateTime NewValidity, string Tenant);