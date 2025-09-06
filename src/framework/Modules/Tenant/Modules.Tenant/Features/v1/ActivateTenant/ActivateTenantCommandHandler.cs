using FSH.Framework.Tenant.Contracts.v1.ActivateTenant;
using FSH.Framework.Tenant.Services;
using FSH.Modules.Common.Core.Messaging.CQRS;

namespace FSH.Modules.Tenant.Features.v1.ActivateTenant;
public sealed class ActivateTenantCommandHandler(ITenantService tenantService)
    : ICommandHandler<ActivateTenantCommand, ActivateTenantCommandResponse>
{
    public async Task<ActivateTenantCommandResponse> HandleAsync(
        ActivateTenantCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await tenantService.ActivateAsync(command.TenantId, cancellationToken);
        return new ActivateTenantCommandResponse(result, command.TenantId);
    }
}