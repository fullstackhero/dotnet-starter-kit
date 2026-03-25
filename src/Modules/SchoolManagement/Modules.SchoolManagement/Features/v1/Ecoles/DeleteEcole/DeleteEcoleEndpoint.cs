using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.SchoolManagement.Contracts.v1.Ecoles.DeleteEcole;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.SchoolManagement.Features.v1.Ecoles.DeleteEcole;

public static class DeleteEcoleEndpoint
{
    public static RouteHandlerBuilder MapDeleteEcoleEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/ecoles/{id:guid}", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
        {
            await mediator.Send(new DeleteEcoleCommand(id), cancellationToken);
            return TypedResults.NoContent();
        })
        .WithName("DeleteEcole")
        .WithSummary("Delete a school")
        .RequirePermission(SchoolManagementPermissionConstants.Ecoles.Delete)
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
    }
}
