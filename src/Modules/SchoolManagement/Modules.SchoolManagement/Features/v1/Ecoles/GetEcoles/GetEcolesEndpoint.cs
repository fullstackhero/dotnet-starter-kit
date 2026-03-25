using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.SchoolManagement.Contracts.DTOs;
using FSH.Modules.SchoolManagement.Contracts.v1.Ecoles.GetEcoles;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.SchoolManagement.Features.v1.Ecoles.GetEcoles;

public static class GetEcolesEndpoint
{
    public static RouteHandlerBuilder MapGetEcolesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/ecoles", async (IMediator mediator,
            int? pageNumber, int? pageSize, string? sort, string? search, string? region, string? type,
            CancellationToken cancellationToken) =>
        {
            var query = new GetEcolesQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Sort = sort,
                Search = search,
                Region = region,
                Type = type
            };
            return TypedResults.Ok(await mediator.Send(query, cancellationToken));
        })
        .WithName("GetEcoles")
        .WithSummary("Get schools list")
        .RequirePermission(SchoolManagementPermissionConstants.Ecoles.View)
        .Produces<PagedResponse<EcoleDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }
}
