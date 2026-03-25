using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.SchoolManagement.Contracts.DTOs;
using FSH.Modules.SchoolManagement.Contracts.v1.Matieres.GetMatieres;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.SchoolManagement.Features.v1.Matieres.GetMatieres;

public static class GetMatieresEndpoint
{
    public static RouteHandlerBuilder MapGetMatieresEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/matieres", async (IMediator mediator, string? search, CancellationToken cancellationToken) =>
            TypedResults.Ok(await mediator.Send(new GetMatieresQuery(search), cancellationToken)))
        .WithName("GetMatieres")
        .WithSummary("Obtenir la liste des matières")
        .RequirePermission(SchoolManagementPermissionConstants.Matieres.View)
        .Produces<IReadOnlyCollection<MatiereDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }
}
