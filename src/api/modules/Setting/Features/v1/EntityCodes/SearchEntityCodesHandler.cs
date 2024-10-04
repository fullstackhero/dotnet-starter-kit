using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Core.Specifications;
using FSH.Starter.WebApi.Setting.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Starter.WebApi.Setting.Features.v1.EntityCodes;

public sealed class SearchEntityCodesHandler(
    [FromKeyedServices("setting:EntityCode")] IReadRepository<EntityCode> repository)
    : IRequestHandler<SearchEntityCodesRequest, PagedList<EntityCodeDto>>
{
    public async Task<PagedList<EntityCodeDto>> Handle(SearchEntityCodesRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var spec = new SearchEntityCodesSpecs(request);

        var items = await repository.ListAsync(spec, cancellationToken).ConfigureAwait(false);
        var totalCount = await repository.CountAsync(spec, cancellationToken).ConfigureAwait(false);

        return new PagedList<EntityCodeDto>(items, request.PageNumber, request.PageSize, totalCount);
    }
}
