using FSH.Modules.Common.Core.Messaging.CQRS;

namespace FSH.Framework.Tenant.Contracts.v1.DisableTenant;
public sealed record DisableTenantCommand(string TenantId) : ICommand<DisableTenantCommandResponse>;