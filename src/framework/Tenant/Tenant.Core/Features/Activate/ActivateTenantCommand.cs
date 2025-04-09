using FSH.Framework.Core.Messaging.CQRS;

namespace FSH.Framework.Tenant.Core.Features.Activate;
public record ActivateTenantCommand(string TenantId) : ICommand<ActivateTenantResponse>;
