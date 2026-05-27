using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Tickets.Contracts.Authorization;
using FSH.Modules.Tickets.Contracts.v1.Tickets;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Tickets.Features.v1.Tickets.AssignTicket;

public static class AssignTicketEndpoint
{
    public sealed record AssignTicketRequest(Guid? AssigneeUserId);

    internal static RouteHandlerBuilder MapAssignTicketEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/tickets/{ticketId:guid}/assign",
                async (Guid ticketId, AssignTicketRequest body, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new AssignTicketCommand(ticketId, body.AssigneeUserId), ct)))
            .WithName("AssignTicket")
            .WithSummary("Assign or reassign a ticket")
            .RequirePermission(TicketsPermissions.Tickets.Assign)
            .WithIdempotency();
    }
}
