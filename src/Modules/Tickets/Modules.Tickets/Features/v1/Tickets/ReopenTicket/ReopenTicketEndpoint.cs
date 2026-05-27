using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Tickets.Contracts.Authorization;
using FSH.Modules.Tickets.Contracts.v1.Tickets;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Tickets.Features.v1.Tickets.ReopenTicket;

public static class ReopenTicketEndpoint
{
    internal static RouteHandlerBuilder MapReopenTicketEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/tickets/{ticketId:guid}/reopen",
                async (Guid ticketId, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new ReopenTicketCommand(ticketId), ct)))
            .WithName("ReopenTicket")
            .WithSummary("Reopen a resolved or closed ticket")
            .RequirePermission(TicketsPermissions.Tickets.Reopen)
            .WithIdempotency();
    }
}
