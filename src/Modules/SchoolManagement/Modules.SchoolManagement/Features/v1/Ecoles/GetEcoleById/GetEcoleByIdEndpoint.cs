using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.SchoolManagement.Contracts.DTOs;
using FSH.Modules.SchoolManagement.Contracts.v1.Ecoles.GetEcoleById;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.SchoolManagement.Features.v1.Ecoles.GetEcoleById;

public static class GetEcoleByIdEndpoint
{
    public static RouteHandlerBuilder MapGetEcoleByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/ecoles/{id:guid}", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
            TypedResults.Ok(await mediator.Send(new GetEcoleByIdQuery(id), cancellationToken)))
        .WithName("GetEcoleById")
        .WithSummary("Get school by ID")
        .RequirePermission(SchoolManagementPermissionConstants.Ecoles.View)
        .Produces<EcoleDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
    }
}
