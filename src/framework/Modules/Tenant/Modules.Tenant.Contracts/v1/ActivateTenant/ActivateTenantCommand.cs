using FSH.Modules.Common.Core.Messaging.CQRS;

namespace FSH.Framework.Tenant.Contracts.v1.ActivateTenant;
public sealed record ActivateTenantCommand(string TenantId) : ICommand<ActivateTenantCommandResponse>;