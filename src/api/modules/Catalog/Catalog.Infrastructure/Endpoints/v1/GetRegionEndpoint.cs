using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Regions.Get.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class GetRegionEndpoint
{
    internal static RouteHandlerBuilder MapGetRegionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet("/regions/{id:guid}", async (Guid id, ISender mediator) =>
            {
                var response = await mediator.Send(new GetRegionRequest(id));
                return Results.Ok(response);
            })
            .WithName(nameof(GetRegionEndpoint))
            .WithSummary("Gets a Region by ID")
            .WithDescription("Gets a Region by ID")
            .Produces<RegionResponse>()
            .RequirePermission("Permissions.Regions.View")
            .MapToApiVersion(1);
    }
}