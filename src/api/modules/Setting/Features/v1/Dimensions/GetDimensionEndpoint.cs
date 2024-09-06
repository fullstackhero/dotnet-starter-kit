using FSH.Framework.Infrastructure.Auth.Policy;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Setting.Features.v1.Dimensions;
public static class GetDimensionEndpoint
{
    internal static RouteHandlerBuilder MapGetDimensionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
                        {
                            var response = await mediator.Send(new GetDimensionRequest(id));
                            return Results.Ok(response);
                        })
                        .WithName(nameof(GetDimensionEndpoint))
                        .WithSummary("gets dimension item by id")
                        .WithDescription("gets dimension item by id")
                        .Produces<GetDimensionResponse>()
                        .RequirePermission("Permissions.Dimensions.View")
                        .MapToApiVersion(1);
    }
}
