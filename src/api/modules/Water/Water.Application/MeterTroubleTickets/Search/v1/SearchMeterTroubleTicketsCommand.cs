using FSH.Framework.Core.Paging;
using FSH.Starter.WebApi.Water.Application.MeterTroubleTickets.Get.v1;
using FSH.Starter.WebApi.Water.Domain;
using MediatR;

namespace FSH.Starter.WebApi.Water.Application.MeterTroubleTickets.Search.v1;

public class SearchMeterTroubleTicketsCommand : PaginationFilter, IRequest<PagedList<MeterTroubleTicketResponse>>
{
    public Guid? MeterId { get; set; }
    public TicketStatus? Status { get; set; }
}
