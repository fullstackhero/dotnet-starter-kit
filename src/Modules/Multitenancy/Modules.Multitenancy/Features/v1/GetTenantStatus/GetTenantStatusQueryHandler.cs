using FSH.Modules.Multitenancy.Contracts;
using FSH.Modules.Multitenancy.Contracts.Dtos;
using FSH.Modules.Multitenancy.Contracts.v1.GetTenantStatus;
using Mediator;

namespace FSH.Modules.Multitenancy.Features.v1.GetTenantStatus;

public sealed class GetTenantStatusQueryHandler(ITenantService tenantService)
    : IQueryHandler<GetTenantStatusQuery, TenantStatusDto>
{
    public async ValueTask<TenantStatusDto> Handle(GetTenantStatusQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await tenantService.GetStatusAsync(query.TenantId, cancellationToken).ConfigureAwait(false);
    }
}