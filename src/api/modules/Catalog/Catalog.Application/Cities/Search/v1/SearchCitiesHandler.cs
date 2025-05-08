using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Application.Cities.Get.v1;
using FSH.Starter.WebApi.Catalog.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;


namespace FSH.Starter.WebApi.Catalog.Application.Cities.Search.v1;
public sealed class SearchCitiesHandler(
    [FromKeyedServices("catalog:Cities")] IReadRepository<City> repository)
    : IRequestHandler<SearchCitiesCommand, PagedList<CityResponse>>
{
    public async Task<PagedList<CityResponse>> Handle(SearchCitiesCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var spec = new SearchCitiespecs(request);

        var items = await repository.ListAsync(spec, cancellationToken).ConfigureAwait(false);
        var totalCount = await repository.CountAsync(spec, cancellationToken).ConfigureAwait(false);

        return new PagedList<CityResponse>(items, request!.PageNumber, request!.PageSize, totalCount);
    }
}

