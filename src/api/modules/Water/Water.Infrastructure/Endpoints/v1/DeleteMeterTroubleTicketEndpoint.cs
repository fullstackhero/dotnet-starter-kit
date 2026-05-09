using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Water.Application.MeterTroubleTickets.Delete.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Water.Infrastructure.Endpoints.v1;
public static class DeleteMeterTroubleTicketEndpoint
{
    internal static RouteHandlerBuilder MapMeterTroubleTicketDeleteEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapDelete("/{id:guid}", async (Guid id, ISender mediator) =>
             {
                 await mediator.Send(new DeleteMeterTroubleTicketCommand(id));
                 return Results.NoContent();
             })
            .WithName(nameof(DeleteMeterTroubleTicketEndpoint))
            .WithSummary("deletes meter trouble ticket by id")
            .WithDescription("deletes meter trouble ticket by id")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("Permissions.MeterTroubleTickets.Delete")
            .MapToApiVersion(1);
    }
}
