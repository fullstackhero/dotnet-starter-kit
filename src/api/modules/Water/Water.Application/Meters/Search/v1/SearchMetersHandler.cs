using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Water.Application.Meters.Get.v1;
using FSH.Starter.WebApi.Water.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Starter.WebApi.Water.Application.Meters.Search.v1;

public sealed class SearchMetersHandler(
    [FromKeyedServices("water:meters")] IReadRepository<Meter> repository)
    : IRequestHandler<SearchMetersCommand, PagedList<MeterResponse>>
{
    public async Task<PagedList<MeterResponse>> Handle(SearchMetersCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var spec = new SearchMeterSpecs(request);
        var items = await repository.ListAsync(spec, cancellationToken).ConfigureAwait(false);
        var totalCount = await repository.CountAsync(spec, cancellationToken).ConfigureAwait(false);
        return new PagedList<MeterResponse>(items, request!.PageNumber, request!.PageSize, totalCount);
    }
}
