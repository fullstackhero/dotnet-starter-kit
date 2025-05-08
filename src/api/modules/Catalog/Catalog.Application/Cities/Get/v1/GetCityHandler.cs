using FSH.Framework.Core.Caching;
using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Domain;
using FSH.Starter.WebApi.Catalog.Domain.Exceptions;
using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Cities.Get.v1;

public sealed class GetCityHandler(
    IReadRepository<City> repository,
    ICacheService cache)
    : IRequestHandler<GetCityRequest, CityResponse>
{
    public async Task<CityResponse> Handle(GetCityRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = await cache.GetOrSetAsync(
            $"city:{request.Id}",
            async () =>
            {
                var cityItem = await repository.GetByIdAsync(request.Id, cancellationToken);
                if (cityItem == null) throw new CityNotFoundException(request.Id);
                return new CityResponse(cityItem.Id, cityItem.Name, cityItem.Description, cityItem.RegionId);
            },
            cancellationToken: cancellationToken);
        return item!;
    }
}
