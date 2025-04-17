using FSH.Framework.Core.Messaging.CQRS;
using FSH.Framework.Tenant.Contracts.v1.DisableTenant;
using FSH.Framework.Tenant.Services;

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
