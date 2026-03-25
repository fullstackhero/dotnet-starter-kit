using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.SchoolManagement.Contracts.v1.Ecoles.UpdateEcole;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.SchoolManagement.Features.v1.Ecoles.UpdateEcole;

public static class UpdateEcoleEndpoint
{
    public static RouteHandlerBuilder MapUpdateEcoleEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/ecoles/{id:guid}", async (Guid id, IMediator mediator, [FromBody] UpdateEcoleRequest request, CancellationToken cancellationToken) =>
        {
            await mediator.Send(new UpdateEcoleCommand(id, request.Nom, request.CodeEcole, request.Type, request.Adresse, request.Telephone, request.Email, request.Region, request.Ville), cancellationToken);
            return TypedResults.NoContent();
        })
        .WithName("UpdateEcole")
        .WithSummary("Update a school")
        .RequirePermission(SchoolManagementPermissionConstants.Ecoles.Update)
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
    }
}

public sealed record UpdateEcoleRequest(
    string Nom,
    string CodeEcole,
    string Type,
    string? Adresse,
    string? Telephone,
    string? Email,
    string? Region,
    string? Ville);
