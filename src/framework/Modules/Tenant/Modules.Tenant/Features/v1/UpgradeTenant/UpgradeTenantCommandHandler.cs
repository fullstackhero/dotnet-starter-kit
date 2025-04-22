using FSH.Framework.Tenant.Contracts.v1.UpgradeTenant;
using FSH.Framework.Tenant.Services;
using FSH.Modules.Common.Core.Messaging.CQRS;

namespace FSH.Framework.Tenant.Features.v1.UpgradeTenant;
internal sealed class UpgradeTenantCommandHandler(ITenantService service)
    : ICommandHandler<UpgradeTenantCommand, UpgradeTenantCommandResponse>
{
    public async Task<UpgradeTenantCommandResponse> HandleAsync(
        UpgradeTenantCommand request,
        CancellationToken cancellationToken = default)
    {
        var validUpto = await service.UpgradeSubscription(request.Tenant, request.ExtendedExpiryDate);
        return new UpgradeTenantCommandResponse(validUpto, request.Tenant);
    }
}