using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Water.Application.Meters.Create.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Water.Infrastructure.Endpoints.v1;
public static class CreateMeterEndpoint
{
    internal static RouteHandlerBuilder MapMeterCreationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/", async (CreateMeterCommand request, ISender mediator) =>
            {
                var response = await mediator.Send(request);
                return Results.Ok(response);
            })
            .WithName(nameof(CreateMeterEndpoint))
            .WithSummary("creates a meter")
            .WithDescription("creates a meter")
            .Produces<CreateMeterResponse>()
            .RequirePermission("Permissions.Meters.Create")
            .MapToApiVersion(1);
    }
}
