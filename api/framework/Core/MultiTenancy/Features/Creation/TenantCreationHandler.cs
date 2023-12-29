using FSH.Framework.Core.MultiTenancy.Abstractions;
using MediatR;

namespace FSH.Framework.Core.MultiTenancy.Features.Creation;
public sealed class TenantCreationHandler(ITenantService service) : IRequestHandler<TenantCreationCommand, string>
{
    public Task<string> Handle(TenantCreationCommand request, CancellationToken cancellationToken)
    {
        return service.CreateAsync(request, cancellationToken);
    }
}
