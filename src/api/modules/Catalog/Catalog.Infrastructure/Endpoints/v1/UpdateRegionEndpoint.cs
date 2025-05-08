using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Regions.Update.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class UpdateRegionEndpoint
{
    internal static RouteHandlerBuilder MapRegionUpdateEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPut("/regions/{id:guid}", async (Guid id, UpdateRegionCommand request, ISender mediator) =>
            {
                if (id != request.Id) return Results.BadRequest();
                var response = await mediator.Send(request);
                return Results.Ok(response);
            })
            .WithName(nameof(UpdateRegionEndpoint))
            .WithSummary("Updates a Region")
            .WithDescription("Updates a Region")
            .Produces<UpdateRegionResponse>()
            .RequirePermission("Permissions.Regions.Update")
            .MapToApiVersion(1);
    }
}
