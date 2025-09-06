using FSH.Modules.Common.Core.Messaging.CQRS;

namespace FSH.Framework.Tenant.Contracts.v1.UpgradeTenant;
public sealed record UpgradeTenantCommand(string Tenant, DateTime ExtendedExpiryDate)
    : ICommand<UpgradeTenantCommandResponse>;