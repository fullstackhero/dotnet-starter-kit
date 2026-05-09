using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Water.Application.Meters.Get.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Water.Infrastructure.Endpoints.v1;
public static class GetMeterEndpoint
{
    internal static RouteHandlerBuilder MapGetMeterEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
            {
                var response = await mediator.Send(new GetMeterRequest(id));
                return Results.Ok(response);
            })
            .WithName(nameof(GetMeterEndpoint))
            .WithSummary("gets meter by id")
            .WithDescription("gets meter by id")
            .Produces<MeterResponse>()
            .RequirePermission("Permissions.Meters.View")
            .MapToApiVersion(1);
    }
}
