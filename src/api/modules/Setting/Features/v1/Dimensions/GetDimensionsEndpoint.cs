using FSH.Framework.Infrastructure.Auth.Policy;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Setting.Features.v1.Dimensions;

public static class GetDimensionsEndpoint
{
    internal static RouteHandlerBuilder MapGetDimensionsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/getlist", async (ISender mediator, [FromBody] GetDimensionsRequest command) =>
            {
                var response = await mediator.Send(command);
                return Results.Ok(response);
            })
            .WithName(nameof(GetDimensionsEndpoint))
            .WithSummary("Gets a list of Dimensions")
            .WithDescription("Gets a list of Dimensions with filtering support")
            .Produces<List<DimensionDto>>()
            .RequirePermission("Permissions.Dimensions.Search")
            .MapToApiVersion(1);
    }
}

