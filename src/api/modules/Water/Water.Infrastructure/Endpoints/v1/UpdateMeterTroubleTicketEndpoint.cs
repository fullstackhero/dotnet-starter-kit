using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Water.Application.MeterTroubleTickets.Update.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Water.Infrastructure.Endpoints.v1;
public static class UpdateMeterTroubleTicketEndpoint
{
    internal static RouteHandlerBuilder MapMeterTroubleTicketUpdateEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPut("/{id:guid}", async (Guid id, UpdateMeterTroubleTicketCommand request, ISender mediator) =>
            {
                if (id != request.Id) return Results.BadRequest();
                var response = await mediator.Send(request);
                return Results.Ok(response);
            })
            .WithName(nameof(UpdateMeterTroubleTicketEndpoint))
            .WithSummary("update a meter trouble ticket")
            .WithDescription("update a meter trouble ticket")
            .Produces<UpdateMeterTroubleTicketResponse>()
            .RequirePermission("Permissions.MeterTroubleTickets.Update")
            .MapToApiVersion(1);
    }
}
