using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.SchoolManagement.Contracts.DTOs;
using FSH.Modules.SchoolManagement.Contracts.v1.Classes.GetClasses;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.SchoolManagement.Features.v1.Classes.GetClasses;

public static class GetClassesEndpoint
{
    public static RouteHandlerBuilder MapGetClassesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/classes", async (IMediator mediator,
            int? pageNumber, int? pageSize, string? sort, Guid? ecoleId, string? niveau, Guid? anneeScolaireId,
            CancellationToken cancellationToken) =>
        {
            var query = new GetClassesQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Sort = sort,
                EcoleId = ecoleId,
                Niveau = niveau,
                AnneeScolaireId = anneeScolaireId
            };
            return TypedResults.Ok(await mediator.Send(query, cancellationToken));
        })
        .WithName("GetClasses")
        .WithSummary("Get classes list")
        .RequirePermission(SchoolManagementPermissionConstants.Classes.View)
        .Produces<PagedResponse<ClasseDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }
}
