using Microsoft.Extensions.DependencyInjection;
using FSH.Starter.WebApi.Catalog.Domain.Exceptions;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Core.Caching;
using FSH.Starter.WebApi.Catalog.Domain;
using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Agencies.Get.v1;
public sealed class GetAgencyHandler(
    [FromKeyedServices("catalog:agencies")] IReadRepository<Agency> repository,
    ICacheService cache)
    : IRequestHandler<GetAgencyRequest, AgencyResponse>
{
    public async Task<AgencyResponse> Handle(GetAgencyRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = await cache.GetOrSetAsync(
            $"agency:{request.Id}",
            async () =>
            {
                var agencyItem = await repository.GetByIdAsync(request.Id, cancellationToken);
                if (agencyItem == null) throw new AgencyNotFoundException(request.Id);
                return new AgencyResponse(agencyItem.Id, agencyItem.Name);
            },
            cancellationToken: cancellationToken);
        return item!;
    }
}
