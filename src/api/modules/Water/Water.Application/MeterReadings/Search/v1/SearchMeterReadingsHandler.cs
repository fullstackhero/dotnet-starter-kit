using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Water.Application.MeterReadings.Get.v1;
using FSH.Starter.WebApi.Water.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Starter.WebApi.Water.Application.MeterReadings.Search.v1;

public sealed class SearchMeterReadingsHandler(
    [FromKeyedServices("water:meter-readings")] IReadRepository<MeterReading> repository)
    : IRequestHandler<SearchMeterReadingsCommand, PagedList<MeterReadingResponse>>
{
    public async Task<PagedList<MeterReadingResponse>> Handle(SearchMeterReadingsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var spec = new SearchMeterReadingSpecs(request);
        var items = await repository.ListAsync(spec, cancellationToken).ConfigureAwait(false);
        var totalCount = await repository.CountAsync(spec, cancellationToken).ConfigureAwait(false);
        return new PagedList<MeterReadingResponse>(items, request!.PageNumber, request!.PageSize, totalCount);
    }
}
