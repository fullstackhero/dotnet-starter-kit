using Microsoft.Extensions.DependencyInjection;
using FSH.Starter.WebApi.Catalog.Domain.Exceptions;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Core.Caching;
using FSH.Starter.WebApi.Catalog.Domain;
using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Neighborhoods.Get.v1;
public sealed class GetNeighborhoodHandler(
    [FromKeyedServices("catalog:neighborhoods")] IReadRepository<Neighborhood> repository,
    ICacheService cache)
    : IRequestHandler<GetNeighborhoodRequest, NeighborhoodResponse>
{
    public async Task<NeighborhoodResponse> Handle(GetNeighborhoodRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = await cache.GetOrSetAsync(
            $"Neighborhood:{request.Id}",
            async () =>
            {
                var NeighborhoodItem = await repository.GetByIdAsync(request.Id, cancellationToken);
                if (NeighborhoodItem == null) throw new NeighborhoodNotFoundException(request.Id);
                return new NeighborhoodResponse(NeighborhoodItem.Id, NeighborhoodItem.Name, NeighborhoodItem.Description, NeighborhoodItem.CityId, NeighborhoodItem.SphereImgURL, NeighborhoodItem.Score);
            },
            cancellationToken: cancellationToken);
        return item!;
    }
}
