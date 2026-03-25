using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.SchoolManagement.Contracts.v1.Ecoles.CreateEcole;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.SchoolManagement.Features.v1.Ecoles.CreateEcole;

public static class CreateEcoleEndpoint
{
    public static RouteHandlerBuilder MapCreateEcoleEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/ecoles", async (IMediator mediator, [FromBody] CreateEcoleCommand request, CancellationToken cancellationToken) =>
        {
            var id = await mediator.Send(request, cancellationToken);
            return TypedResults.Created($"/api/v1/school/ecoles/{id}", id);
        })
        .WithName("CreateEcole")
        .WithSummary("Créer une nouvelle école")
        .RequirePermission(SchoolManagementPermissionConstants.Ecoles.Create)
        .Produces<Guid>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }
}
