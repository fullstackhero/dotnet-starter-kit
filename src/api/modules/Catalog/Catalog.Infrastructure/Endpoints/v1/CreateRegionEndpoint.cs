using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Regions.Create.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class CreateRegionEndpoint
{
    internal static RouteHandlerBuilder MapRegionCreationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/regions", async (CreateRegionCommand request, ISender mediator) =>
            {
                var response = await mediator.Send(request);
                return Results.Ok(response);
            })
            .WithName(nameof(CreateRegionEndpoint))
            .WithSummary("Creates a Region")
            .WithDescription("Creates a Region")
            .Produces<CreateRegionResponse>()
            .RequirePermission("Permissions.Regions.Create")
            .MapToApiVersion(1);
    }
}