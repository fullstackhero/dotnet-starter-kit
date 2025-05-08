using FSH.Framework.Core.Caching;
using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Domain;
using FSH.Starter.WebApi.Catalog.Domain.Exceptions;
using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Regions.Get.v1;

public sealed class GetRegionHandler(
    IReadRepository<Region> repository,
    ICacheService cache)
    : IRequestHandler<GetRegionRequest, RegionResponse>
{
    public async Task<RegionResponse> Handle(GetRegionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = await cache.GetOrSetAsync(
            $"region:{request.Id}",
            async () =>
            {
                var regionItem = await repository.GetByIdAsync(request.Id, cancellationToken);
                if (regionItem == null) throw new RegionNotFoundException(request.Id);
                return new RegionResponse(regionItem.Id, regionItem.Name, regionItem.Description);
            },
            cancellationToken: cancellationToken);
        return item!;
    }
}