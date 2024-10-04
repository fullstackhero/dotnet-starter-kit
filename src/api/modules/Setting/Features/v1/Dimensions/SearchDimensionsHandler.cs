using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Setting.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Starter.WebApi.Setting.Features.v1.Dimensions;

public sealed class SearchDimensionsHandler(
    [FromKeyedServices("setting:dimension")] IReadRepository<Dimension> repository)
    : IRequestHandler<SearchDimensionsRequest, PagedList<DimensionDto>>
{
    public async Task<PagedList<DimensionDto>> Handle(SearchDimensionsRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var spec = new SearchDimensionsSpecs(request);

        var items = await repository.ListAsync(spec, cancellationToken).ConfigureAwait(false);
        var totalCount = await repository.CountAsync(spec, cancellationToken).ConfigureAwait(false);

        return new PagedList<DimensionDto>(items, request!.PageNumber, request!.PageSize, totalCount);
    }
}
