using Asp.Versioning;
using FSH.Framework.Infrastructure.Auth.Policy;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Setting.Features.v1.EntityCodes;
public static class DeleteEntityCodeEndpoint
{
    internal static RouteHandlerBuilder MapDeleteEntityCodeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapDelete("/{id:guid}", async (Guid id, ISender mediator) =>
            {
                await mediator.Send(new DeleteEntityCodeCommand(id));
                return Results.NoContent();
            })
            .WithName(nameof(DeleteEntityCodeEndpoint))
            .WithSummary("Deletes a EntityCode item")
            .WithDescription("Deleted a EntityCode item")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("Permissions.EntityCodes.Delete")
            .MapToApiVersion(new ApiVersion(1, 0));

    }
}
