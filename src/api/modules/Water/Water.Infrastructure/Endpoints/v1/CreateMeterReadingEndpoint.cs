using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Water.Application.MeterReadings.Create.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Water.Infrastructure.Endpoints.v1;
public static class CreateMeterReadingEndpoint
{
    internal static RouteHandlerBuilder MapMeterReadingCreationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/", async (CreateMeterReadingCommand request, ISender mediator) =>
            {
                var response = await mediator.Send(request);
                return Results.Ok(response);
            })
            .WithName(nameof(CreateMeterReadingEndpoint))
            .WithSummary("creates a meter reading")
            .WithDescription("creates a meter reading")
            .Produces<CreateMeterReadingResponse>()
            .RequirePermission("Permissions.MeterReadings.Create")
            .MapToApiVersion(1);
    }
}
