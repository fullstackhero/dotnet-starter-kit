using FSH.Framework.Core.Caching;
using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Domain;
using FSH.Starter.WebApi.Catalog.Domain.Exceptions;
using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Neighborhoods.Get.v1;

public sealed class GetNeighborhoodHandler(
    IReadRepository<Neighborhood> repository,
    ICacheService cache)
    : IRequestHandler<GetNeighborhoodRequest, NeighborhoodResponse>
{
    public async Task<NeighborhoodResponse> Handle(GetNeighborhoodRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = await cache.GetOrSetAsync(
            $"neighborhood:{request.Id}",
            async () =>
            {
                var neighborhoodItem = await repository.GetByIdAsync(request.Id, cancellationToken);
                if (neighborhoodItem == null) throw new NeighborhoodNotFoundException(request.Id);
                return new NeighborhoodResponse(neighborhoodItem.Id, neighborhoodItem.Name, neighborhoodItem.Description, neighborhoodItem.CityId, neighborhoodItem.SphereImgURL, neighborhoodItem.Score);
            },
            cancellationToken: cancellationToken);
        return item!;
    }
}
