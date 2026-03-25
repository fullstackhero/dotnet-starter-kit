using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.SchoolManagement.Contracts.DTOs;
using FSH.Modules.SchoolManagement.Contracts.v1.AnneeScolaires.GetAnneeScolaires;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.SchoolManagement.Features.v1.AnneeScolaires.GetAnneeScolaires;

public static class GetAnneeScolairesEndpoint
{
    public static RouteHandlerBuilder MapGetAnneeScolairesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/annees-scolaires", async (IMediator mediator, CancellationToken cancellationToken) =>
            TypedResults.Ok(await mediator.Send(new GetAnneeScolairesQuery(), cancellationToken)))
        .WithName("GetAnneeScolaires")
        .WithSummary("Get school years list")
        .RequirePermission(SchoolManagementPermissionConstants.AnneeScolaires.View)
        .Produces<IReadOnlyCollection<AnneeScolaireDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }
}
