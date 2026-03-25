using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.SchoolManagement.Contracts.v1.AnneeScolaires.UpdateAnneeScolaire;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.SchoolManagement.Features.v1.AnneeScolaires.UpdateAnneeScolaire;

public static class UpdateAnneeScolaireEndpoint
{
    public static RouteHandlerBuilder MapUpdateAnneeScolaireEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/annees-scolaires/{id:guid}", async (Guid id, IMediator mediator, [FromBody] UpdateAnneeScolaireRequest request, CancellationToken cancellationToken) =>
        {
            await mediator.Send(new UpdateAnneeScolaireCommand(id, request.Libelle, request.DateDebut, request.DateFin, request.EstActive), cancellationToken);
            return TypedResults.NoContent();
        })
        .WithName("UpdateAnneeScolaire")
        .WithSummary("Update a school year")
        .RequirePermission(SchoolManagementPermissionConstants.AnneeScolaires.Update)
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
    }
}

public sealed record UpdateAnneeScolaireRequest(string Libelle, DateTimeOffset DateDebut, DateTimeOffset DateFin, bool EstActive);
