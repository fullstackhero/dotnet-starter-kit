using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Water.Application.MeterTroubleTickets.Create.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Water.Infrastructure.Endpoints.v1;
public static class CreateMeterTroubleTicketEndpoint
{
    internal static RouteHandlerBuilder MapMeterTroubleTicketCreationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/", async (CreateMeterTroubleTicketCommand request, ISender mediator) =>
            {
                var response = await mediator.Send(request);
                return Results.Ok(response);
            })
            .WithName(nameof(CreateMeterTroubleTicketEndpoint))
            .WithSummary("creates a meter trouble ticket")
            .WithDescription("creates a meter trouble ticket")
            .Produces<CreateMeterTroubleTicketResponse>()
            .RequirePermission("Permissions.MeterTroubleTickets.Create")
            .MapToApiVersion(1);
    }
}
