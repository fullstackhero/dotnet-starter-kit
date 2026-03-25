using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.SchoolManagement.Contracts.v1.Classes.UpdateClasse;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.SchoolManagement.Features.v1.Classes.UpdateClasse;

public static class UpdateClasseEndpoint
{
    public static RouteHandlerBuilder MapUpdateClasseEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/classes/{id:guid}", async (Guid id, IMediator mediator, [FromBody] UpdateClasseRequest request, CancellationToken cancellationToken) =>
        {
            await mediator.Send(new UpdateClasseCommand(id, request.Nom, request.Niveau, request.Capacite), cancellationToken);
            return TypedResults.NoContent();
        })
        .WithName("UpdateClasse")
        .WithSummary("Mettre à jour une classe")
        .RequirePermission(SchoolManagementPermissionConstants.Classes.Update)
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
    }
}

public sealed record UpdateClasseRequest(string Nom, string Niveau, int Capacite);
