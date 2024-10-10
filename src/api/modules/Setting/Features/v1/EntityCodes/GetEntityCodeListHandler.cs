using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Core.Specifications;
using FSH.Starter.WebApi.Setting.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Starter.WebApi.Setting.Features.v1.EntityCodes;

public sealed class GetEntityCodeListHandler(
    [FromKeyedServices("setting:EntityCode")] IReadRepository<EntityCode> repository)
    : IRequestHandler<GetEntityCodeListRequest, PagedList<EntityCodeDto>>
{
    public async Task<PagedList<EntityCodeDto>> Handle(GetEntityCodeListRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var spec = new EntitiesByPaginationFilterSpec<EntityCode, EntityCodeDto>(request.filter);

        var items = await repository.ListAsync(spec, cancellationToken).ConfigureAwait(false);
        var totalCount = await repository.CountAsync(spec, cancellationToken).ConfigureAwait(false);

        return new PagedList<EntityCodeDto>(items, request.filter.PageNumber, request.filter.PageSize, totalCount);
    }
}
