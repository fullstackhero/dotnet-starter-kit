using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.SchoolManagement.Contracts.DTOs;
using FSH.Modules.SchoolManagement.Contracts.v1.AnneeScolaires.GetAnneeScolaireActive;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.SchoolManagement.Features.v1.AnneeScolaires.GetAnneeScolaireActive;

public static class GetAnneeScolaireActiveEndpoint
{
    public static RouteHandlerBuilder MapGetAnneeScolaireActiveEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/annees-scolaires/active", async (IMediator mediator, CancellationToken cancellationToken) =>
            TypedResults.Ok(await mediator.Send(new GetAnneeScolaireActiveQuery(), cancellationToken)))
        .WithName("GetAnneeScolaireActive")
        .WithSummary("Get active school year")
        .RequirePermission(SchoolManagementPermissionConstants.AnneeScolaires.View)
        .Produces<AnneeScolaireDto?>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }
}
