using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Regions.Delete.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class DeleteRegionEndpoint
{
    internal static RouteHandlerBuilder MapRegionDeleteEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapDelete("/regions/{id:guid}", async (Guid id, ISender mediator) =>
            {
                await mediator.Send(new DeleteRegionCommand(id));
                return Results.NoContent();
            })
            .WithName(nameof(DeleteRegionEndpoint))
            .WithSummary("Deletes a Region by ID")
            .WithDescription("Deletes a Region by ID")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("Permissions.Regions.Delete")
            .MapToApiVersion(1);
    }
}
