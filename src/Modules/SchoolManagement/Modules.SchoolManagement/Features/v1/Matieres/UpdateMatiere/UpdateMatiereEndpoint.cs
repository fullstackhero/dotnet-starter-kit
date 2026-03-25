using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.SchoolManagement.Contracts.v1.Matieres.UpdateMatiere;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.SchoolManagement.Features.v1.Matieres.UpdateMatiere;

public static class UpdateMatiereEndpoint
{
    public static RouteHandlerBuilder MapUpdateMatiereEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/matieres/{id:guid}", async (Guid id, IMediator mediator, [FromBody] UpdateMatiereRequest request, CancellationToken cancellationToken) =>
        {
            await mediator.Send(new UpdateMatiereCommand(id, request.Nom, request.Code, request.Coefficient, request.Description), cancellationToken);
            return TypedResults.NoContent();
        })
        .WithName("UpdateMatiere")
        .WithSummary("Update a subject")
        .RequirePermission(SchoolManagementPermissionConstants.Matieres.Update)
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
    }
}

public sealed record UpdateMatiereRequest(string Nom, string Code, int Coefficient, string? Description);
