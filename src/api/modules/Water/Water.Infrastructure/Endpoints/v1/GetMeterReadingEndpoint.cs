using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Water.Application.MeterReadings.Get.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Water.Infrastructure.Endpoints.v1;
public static class GetMeterReadingEndpoint
{
    internal static RouteHandlerBuilder MapGetMeterReadingEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
            {
                var response = await mediator.Send(new GetMeterReadingRequest(id));
                return Results.Ok(response);
            })
            .WithName(nameof(GetMeterReadingEndpoint))
            .WithSummary("gets meter reading by id")
            .WithDescription("gets meter reading by id")
            .Produces<MeterReadingResponse>()
            .RequirePermission("Permissions.MeterReadings.View")
            .MapToApiVersion(1);
    }
}
