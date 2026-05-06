using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Tickets.Contracts.Authorization;
using FSH.Modules.Tickets.Contracts.v1.Tickets;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Tickets.Features.v1.Tickets.GetTicketById;

public static class GetTicketByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetTicketByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/tickets/{ticketId:guid}",
                async (Guid ticketId, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GetTicketByIdQuery(ticketId), ct)))
            .WithName("GetTicketById")
            .WithSummary("Get a ticket by id")
            .RequirePermission(TicketsPermissions.Tickets.View);
    }
}
