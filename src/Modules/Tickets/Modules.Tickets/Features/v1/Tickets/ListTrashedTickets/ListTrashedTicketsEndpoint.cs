using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Tickets.Contracts.Authorization;
using FSH.Modules.Tickets.Contracts.v1.Tickets;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Tickets.Features.v1.Tickets.ListTrashedTickets;

public static class ListTrashedTicketsEndpoint
{
    internal static RouteHandlerBuilder MapListTrashedTicketsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/tickets/trash",
                async (int? pageNumber, int? pageSize, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new ListTrashedTicketsQuery(pageNumber ?? 1, pageSize ?? 20), ct)))
            .WithName("ListTrashedTickets")
            .WithSummary("List soft-deleted tickets")
            .RequirePermission(TicketsPermissions.Tickets.Restore);
    }
}
