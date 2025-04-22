using FSH.Framework.Tenant.Contracts.v1.CreateTenant;
using FSH.Framework.Tenant.Services;
using FSH.Modules.Common.Core.Messaging.CQRS;

namespace FSH.Framework.Tenant.Features.v1.CreateTenant;
public class CreateTenantCommandHandler(ITenantService service)
    : ICommandHandler<CreateTenantCommand, CreateTenantCommandResponse>
{
    public async Task<CreateTenantCommandResponse> HandleAsync(
        CreateTenantCommand command,
        CancellationToken cancellationToken = default)
    {
        var tenantId = await service.CreateAsync(command.Id,
            command.Name,
            command.ConnectionString,
            command.AdminEmail,
            command.Issuer,
            cancellationToken);
        return new CreateTenantCommandResponse(tenantId);
    }
}