using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Application.Neighborhoods.Get.v1;
using FSH.Starter.WebApi.Catalog.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Starter.WebApi.Catalog.Application.Neighborhoods.Search.v1;
public sealed class SearchNeighborhoodsHandler(
    [FromKeyedServices("catalog:neighborhoods")] IReadRepository<Neighborhood> repository)
    : IRequestHandler<SearchNeighborhoodsCommand, PagedList<NeighborhoodResponse>>
{
    public async Task<PagedList<NeighborhoodResponse>> Handle(SearchNeighborhoodsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var spec = new SearchNeighborhoodSpecs(request);

        var items = await repository.ListAsync(spec, cancellationToken).ConfigureAwait(false);
        var totalCount = await repository.CountAsync(spec, cancellationToken).ConfigureAwait(false);

        return new PagedList<NeighborhoodResponse>(items, request!.PageNumber, request!.PageSize, totalCount);
    }
}
