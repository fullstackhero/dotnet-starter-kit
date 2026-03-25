using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.SchoolManagement.Contracts.DTOs;
using FSH.Modules.SchoolManagement.Contracts.v1.Classes.GetClasseById;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.SchoolManagement.Features.v1.Classes.GetClasseById;

public static class GetClasseByIdEndpoint
{
    public static RouteHandlerBuilder MapGetClasseByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/classes/{id:guid}", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
            TypedResults.Ok(await mediator.Send(new GetClasseByIdQuery(id), cancellationToken)))
        .WithName("GetClasseById")
        .WithSummary("Get class by ID")
        .RequirePermission(SchoolManagementPermissionConstants.Classes.View)
        .Produces<ClasseDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
    }
}
