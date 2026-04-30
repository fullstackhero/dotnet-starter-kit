using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Tickets.Contracts.Authorization;
using FSH.Modules.Tickets.Contracts.v1.Tickets;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Tickets.Features.v1.Tickets.ResolveTicket;

public static class ResolveTicketEndpoint
{
    public sealed record ResolveTicketRequest(string? ResolutionNote);

    internal static RouteHandlerBuilder MapResolveTicketEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/tickets/{ticketId:guid}/resolve",
                async (Guid ticketId, ResolveTicketRequest? body, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new ResolveTicketCommand(ticketId, body?.ResolutionNote), ct)))
            .WithName("ResolveTicket")
            .WithSummary("Mark a ticket as resolved")
            .RequirePermission(TicketsPermissions.Tickets.Resolve)
            .WithIdempotency();
    }
}
