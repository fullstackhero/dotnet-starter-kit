using FSH.Framework.Core.Tenant.Abstractions;
using MediatR;

namespace FSH.Framework.Core.Tenant.Features.RegisterTenant;
public sealed class RegisterTenantHandler(ITenantService service) : IRequestHandler<RegisterTenantCommand, string>
{
    public Task<string> Handle(RegisterTenantCommand request, CancellationToken cancellationToken)
    {
        return service.CreateAsync(request, cancellationToken);
    }
}
