using FSH.Modules.Multitenancy.Contracts;
using FSH.Modules.Multitenancy.Contracts.v1.CreateTenant;
using FSH.Modules.Multitenancy.Provisioning;
using Mediator;

namespace FSH.Modules.Multitenancy.Features.v1.CreateTenant;

public class CreateTenantCommandHandler(ITenantService tenantService, ITenantProvisioningService provisioningService)
    : ICommandHandler<CreateTenantCommand, CreateTenantCommandResponse>
{
    public async ValueTask<CreateTenantCommandResponse> Handle(CreateTenantCommand command, CancellationToken cancellationToken)
    {
        var tenantId = await tenantService.CreateAsync(
            command.Id,
            command.Name,
            command.ConnectionString,
            command.AdminEmail,
            command.Issuer,
            cancellationToken);

        var provisioning = await provisioningService.StartAsync(tenantId, cancellationToken);

        return new CreateTenantCommandResponse(
            tenantId,
            provisioning.CorrelationId,
            provisioning.Status.ToString());
    }
}
