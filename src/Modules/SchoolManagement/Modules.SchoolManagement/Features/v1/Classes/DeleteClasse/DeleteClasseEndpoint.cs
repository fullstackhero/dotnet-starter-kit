using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.SchoolManagement.Contracts.v1.Classes.DeleteClasse;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.SchoolManagement.Features.v1.Classes.DeleteClasse;

public static class DeleteClasseEndpoint
{
    public static RouteHandlerBuilder MapDeleteClasseEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/classes/{id:guid}", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
        {
            await mediator.Send(new DeleteClasseCommand(id), cancellationToken);
            return TypedResults.NoContent();
        })
        .WithName("DeleteClasse")
        .WithSummary("Supprimer une classe")
        .RequirePermission(SchoolManagementPermissionConstants.Classes.Delete)
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
    }
}
