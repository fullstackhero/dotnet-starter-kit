using FSH.Modules.Multitenancy.Contracts.Dtos;
using FSH.Modules.Multitenancy.Contracts.v1.TenantProvisioning;
using FSH.Modules.Multitenancy.Provisioning;
using Mediator;

namespace FSH.Modules.Multitenancy.Features.v1.TenantProvisioning.RetryTenantProvisioning;

public sealed class RetryTenantProvisioningCommandHandler(ITenantProvisioningService provisioningService)
    : ICommandHandler<RetryTenantProvisioningCommand, TenantProvisioningStatusDto>
{
    public async ValueTask<TenantProvisioningStatusDto> Handle(RetryTenantProvisioningCommand command, CancellationToken cancellationToken)
    {
        var correlationId = await provisioningService.RetryAsync(command.TenantId, cancellationToken).ConfigureAwait(false);
        var status = await provisioningService.GetStatusAsync(command.TenantId, cancellationToken).ConfigureAwait(false);
        return status with { CorrelationId = correlationId };
    }
}
