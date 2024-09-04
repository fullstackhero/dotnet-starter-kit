using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Core.Specifications;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Starter.WebApi.Setting.Dimension.Features.v1;

public sealed class GetDimensionListHandler(
    [FromKeyedServices("setting:dimension")] IReadRepository<Dimension.Domain.Dimension> repository)
    : IRequestHandler<GetDimensionListRequest, PagedList<DimensionDto>>
{
    public async Task<PagedList<DimensionDto>> Handle(GetDimensionListRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var spec = new EntitiesByPaginationFilterSpec<Dimension.Domain.Dimension, DimensionDto>(request.filter);

        var items = await repository.ListAsync(spec, cancellationToken).ConfigureAwait(false);
        var totalCount = await repository.CountAsync(spec, cancellationToken).ConfigureAwait(false);

        return new PagedList<DimensionDto>(items, request.filter.PageNumber, request.filter.PageSize, totalCount);
    }
}
