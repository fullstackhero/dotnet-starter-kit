using Ardalis.Specification;
using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Specifications;
using FSH.Starter.WebApi.Water.Application.MeterTroubleTickets.Get.v1;
using FSH.Starter.WebApi.Water.Domain;

namespace FSH.Starter.WebApi.Water.Application.MeterTroubleTickets.Search.v1;

public class SearchMeterTroubleTicketSpecs : EntitiesByPaginationFilterSpec<MeterTroubleTicket, MeterTroubleTicketResponse>
{
    public SearchMeterTroubleTicketSpecs(SearchMeterTroubleTicketsCommand command)
        : base(command) =>
        Query
            .OrderByDescending(c => c.ReportedDate, !command.HasOrderBy())
            .Where(t => t.MeterId == command.MeterId, command.MeterId.HasValue)
            .Where(t => t.Status == command.Status, command.Status.HasValue);
}
