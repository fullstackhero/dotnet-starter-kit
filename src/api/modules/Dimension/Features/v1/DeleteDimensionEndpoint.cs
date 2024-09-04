using Asp.Versioning;
using FSH.Framework.Infrastructure.Auth.Policy;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Setting.Dimension.Features.v1;
public static class DeleteDimensionEndpoint
{
    internal static RouteHandlerBuilder MapDimensionDeletionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapDelete("/{id:guid}", async (Guid id, ISender mediator) =>
            {
                await mediator.Send(new DeleteDimensionCommand(id));
                return Results.NoContent();
            })
            .WithName(nameof(DeleteDimensionEndpoint))
            .WithSummary("Deletes a dimension item")
            .WithDescription("Deleted a dimension item")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("Permissions.Dimensions.Delete")
            .MapToApiVersion(new ApiVersion(1, 0));

    }
}
