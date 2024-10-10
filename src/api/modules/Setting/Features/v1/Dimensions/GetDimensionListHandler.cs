using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Core.Specifications;
using FSH.Starter.WebApi.Setting.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Starter.WebApi.Setting.Features.v1.Dimensions;

public sealed class GetDimensionListHandler(
    [FromKeyedServices("setting:dimension")] IReadRepository<Dimension> repository)
    : IRequestHandler<GetDimensionListRequest, PagedList<DimensionDto>>
{
    public async Task<PagedList<DimensionDto>> Handle(GetDimensionListRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var spec = new EntitiesByPaginationFilterSpec<Dimension, DimensionDto>(request.filter);

        var items = await repository.ListAsync(spec, cancellationToken).ConfigureAwait(false);
        var totalCount = await repository.CountAsync(spec, cancellationToken).ConfigureAwait(false);

        return new PagedList<DimensionDto>(items, request.filter.PageNumber, request.filter.PageSize, totalCount);
    }
}
