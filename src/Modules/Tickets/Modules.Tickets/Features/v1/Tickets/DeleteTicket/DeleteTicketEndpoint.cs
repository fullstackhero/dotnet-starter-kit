using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Tickets.Contracts.Authorization;
using FSH.Modules.Tickets.Contracts.v1.Tickets;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Tickets.Features.v1.Tickets.DeleteTicket;

public static class DeleteTicketEndpoint
{
    internal static RouteHandlerBuilder MapDeleteTicketEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/tickets/{ticketId:guid}",
                async (Guid ticketId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteTicketCommand(ticketId), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteTicket")
            .WithSummary("Soft-delete a ticket (restorable from trash)")
            .RequirePermission(TicketsPermissions.Tickets.Delete);
    }
}
