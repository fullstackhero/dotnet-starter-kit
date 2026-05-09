using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Water.Application.MeterTroubleTickets.Get.v1;
using FSH.Starter.WebApi.Water.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Starter.WebApi.Water.Application.MeterTroubleTickets.Search.v1;

public sealed class SearchMeterTroubleTicketsHandler(
    [FromKeyedServices("water:trouble-tickets")] IReadRepository<MeterTroubleTicket> repository)
    : IRequestHandler<SearchMeterTroubleTicketsCommand, PagedList<MeterTroubleTicketResponse>>
{
    public async Task<PagedList<MeterTroubleTicketResponse>> Handle(SearchMeterTroubleTicketsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var spec = new SearchMeterTroubleTicketSpecs(request);
        var items = await repository.ListAsync(spec, cancellationToken).ConfigureAwait(false);
        var totalCount = await repository.CountAsync(spec, cancellationToken).ConfigureAwait(false);
        return new PagedList<MeterTroubleTicketResponse>(items, request!.PageNumber, request!.PageSize, totalCount);
    }
}
