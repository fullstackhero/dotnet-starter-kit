using Microsoft.Extensions.DependencyInjection;
using FSH.Starter.WebApi.Catalog.Domain.Exceptions;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Core.Caching;
using FSH.Starter.WebApi.Catalog.Domain;
using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Cities.Get.v1;
public sealed class GetCityHandler(
    [FromKeyedServices("catalog:cities")] IReadRepository<City> repository,
    ICacheService cache)
    : IRequestHandler<GetCityRequest, CityResponse>
{
    public async Task<CityResponse> Handle(GetCityRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = await cache.GetOrSetAsync(
            $"City:{request.Id}",
            async () =>
            {
                var CityItem = await repository.GetByIdAsync(request.Id, cancellationToken);
                if (CityItem == null) throw new CityNotFoundException(request.Id);
                return new CityResponse(CityItem.Id, CityItem.Name, CityItem.Description, CityItem.RegionId);
            },
            cancellationToken: cancellationToken);
        return item!;
    }
}
