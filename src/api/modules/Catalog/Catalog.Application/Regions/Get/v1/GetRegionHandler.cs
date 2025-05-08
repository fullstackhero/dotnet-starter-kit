using Microsoft.Extensions.DependencyInjection;
using FSH.Starter.WebApi.Catalog.Domain.Exceptions;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Core.Caching;
using FSH.Starter.WebApi.Catalog.Domain;
using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Regions.Get.v1;
public sealed class GetRegionHandler(
    [FromKeyedServices("catalog:regions")] IReadRepository<Region> repository,
    ICacheService cache)
    : IRequestHandler<GetRegionRequest, RegionResponse>
{
    public async Task<RegionResponse> Handle(GetRegionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = await cache.GetOrSetAsync(
            $"Region:{request.Id}",
            async () =>
            {
                var RegionItem = await repository.GetByIdAsync(request.Id, cancellationToken);
                if (RegionItem == null) throw new RegionNotFoundException(request.Id);
                return new RegionResponse(RegionItem.Id, RegionItem.Name, RegionItem.Description);
            },
            cancellationToken: cancellationToken);
        return item!;
    }
}
