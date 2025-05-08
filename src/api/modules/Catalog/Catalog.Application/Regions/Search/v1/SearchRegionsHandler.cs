using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Application.Regions.Get.v1;
using FSH.Starter.WebApi.Catalog.Domain;
using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Regions.Search.v1;

public sealed class SearchRegionsHandler(
    IReadRepository<Region> repository)
    : IRequestHandler<SearchRegionsCommand, PagedList<RegionResponse>>
{
    public async Task<PagedList<RegionResponse>> Handle(SearchRegionsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var spec = new SearchRegionSpecs(request);

        var items = await repository.ListAsync(spec, cancellationToken).ConfigureAwait(false);
        var totalCount = await repository.CountAsync(spec, cancellationToken).ConfigureAwait(false);

        return new PagedList<RegionResponse>(items, request!.PageNumber, request!.PageSize, totalCount);
    }
}
