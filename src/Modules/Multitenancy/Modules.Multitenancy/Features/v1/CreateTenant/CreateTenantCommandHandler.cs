using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Multitenancy.Contracts;
using FSH.Modules.Multitenancy.Contracts.v1.CreateTenant;
using FSH.Modules.Multitenancy.Provisioning;
using Mediator;

namespace FSH.Modules.Multitenancy.Features.v1.CreateTenant;

public sealed class CreateTenantCommandHandler(
    ITenantService tenantService,
    ITenantProvisioningService provisioningService,
    ITenantInitialPasswordBuffer passwordBuffer)
    : ICommandHandler<CreateTenantCommand, CreateTenantCommandResponse>
{
    public async ValueTask<CreateTenantCommandResponse> Handle(CreateTenantCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tenantId = await tenantService.CreateAsync(
            command.Id,
            command.Name,
            command.ConnectionString,
            command.AdminEmail,
            command.Issuer,
            cancellationToken);

        // Buffer the admin password so IdentityDbInitializer can consume it during
        // the background provisioning seed step. Stored before StartAsync to avoid
        // a race where the seed runs before the buffer is populated.
        passwordBuffer.Store(tenantId, command.AdminPassword);

        var provisioning = await provisioningService.StartAsync(tenantId, cancellationToken);

        return new CreateTenantCommandResponse(
            tenantId,
            provisioning.CorrelationId,
            provisioning.Status.ToString());
    }
}