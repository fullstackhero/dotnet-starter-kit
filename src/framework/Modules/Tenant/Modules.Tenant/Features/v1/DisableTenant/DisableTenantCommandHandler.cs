using FSH.Framework.Tenant.Contracts.v1.DisableTenant;
using FSH.Framework.Tenant.Services;
using FSH.Modules.Common.Core.Messaging.CQRS;

namespace FSH.Framework.Tenant.Features.v1.DisableTenant;
public class DisableTenantCommandHandler(ITenantService service)
    : ICommandHandler<DisableTenantCommand, DisableTenantCommandResponse>
{
    public async Task<DisableTenantCommandResponse> HandleAsync(
        DisableTenantCommand request,
        CancellationToken cancellationToken = default)
    {
        var status = await service.DeactivateAsync(request.TenantId);
        return new DisableTenantCommandResponse(status);
    }
}