using FSH.Framework.Core.Paging;
using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Water.Application.MeterTroubleTickets.Get.v1;
using FSH.Starter.WebApi.Water.Application.MeterTroubleTickets.Search.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Water.Infrastructure.Endpoints.v1;

public static class SearchMeterTroubleTicketsEndpoint
{
    internal static RouteHandlerBuilder MapGetMeterTroubleTicketListEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/search", async (ISender mediator, [FromBody] SearchMeterTroubleTicketsCommand command) =>
            {
                var response = await mediator.Send(command);
                return Results.Ok(response);
            })
            .WithName(nameof(SearchMeterTroubleTicketsEndpoint))
            .WithSummary("Gets a list of meter trouble tickets")
            .WithDescription("Gets a list of meter trouble tickets with pagination and filtering support")
            .Produces<PagedList<MeterTroubleTicketResponse>>()
            .RequirePermission("Permissions.MeterTroubleTickets.View")
            .MapToApiVersion(1);
    }
}
