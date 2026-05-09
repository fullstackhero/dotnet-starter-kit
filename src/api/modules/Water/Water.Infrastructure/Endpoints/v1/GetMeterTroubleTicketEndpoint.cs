using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Water.Application.MeterTroubleTickets.Get.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Water.Infrastructure.Endpoints.v1;
public static class GetMeterTroubleTicketEndpoint
{
    internal static RouteHandlerBuilder MapGetMeterTroubleTicketEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
            {
                var response = await mediator.Send(new GetMeterTroubleTicketRequest(id));
                return Results.Ok(response);
            })
            .WithName(nameof(GetMeterTroubleTicketEndpoint))
            .WithSummary("gets meter trouble ticket by id")
            .WithDescription("gets meter trouble ticket by id")
            .Produces<MeterTroubleTicketResponse>()
            .RequirePermission("Permissions.MeterTroubleTickets.View")
            .MapToApiVersion(1);
    }
}
