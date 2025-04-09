using FSH.Framework.Core.Messaging.CQRS;
using FSH.Framework.Tenant.Core.Abstractions;

namespace FSH.Framework.Tenant.Endpoints.v1.Activate;
public static partial class ActivateTenant
{
    public sealed class Handler(ITenantService tenantService) : ICommandHandler<Command, Response>
    {
        public async Task<Response> HandleAsync(Command command, CancellationToken cancellationToken = default)
        {
            var result = await tenantService.ActivateAsync(command.TenantId, cancellationToken);
            return new Response(result);
        }
    }
}

