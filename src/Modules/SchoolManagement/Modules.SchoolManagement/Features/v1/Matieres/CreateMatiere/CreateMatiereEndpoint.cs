using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.SchoolManagement.Contracts.v1.Matieres.CreateMatiere;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.SchoolManagement.Features.v1.Matieres.CreateMatiere;

public static class CreateMatiereEndpoint
{
    public static RouteHandlerBuilder MapCreateMatiereEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/matieres", async (IMediator mediator, [FromBody] CreateMatiereCommand request, CancellationToken cancellationToken) =>
        {
            var id = await mediator.Send(request, cancellationToken);
            return TypedResults.Created($"/api/v1/school/matieres/{id}", id);
        })
        .WithName("CreateMatiere")
        .WithSummary("Create a new subject")
        .RequirePermission(SchoolManagementPermissionConstants.Matieres.Create)
        .Produces<Guid>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }
}
