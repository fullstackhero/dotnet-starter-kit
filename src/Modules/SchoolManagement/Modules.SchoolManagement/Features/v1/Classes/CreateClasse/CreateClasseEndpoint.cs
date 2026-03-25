using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.SchoolManagement.Contracts.v1.Classes.CreateClasse;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.SchoolManagement.Features.v1.Classes.CreateClasse;

public static class CreateClasseEndpoint
{
    public static RouteHandlerBuilder MapCreateClasseEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/classes", async (IMediator mediator, [FromBody] CreateClasseCommand request, CancellationToken cancellationToken) =>
        {
            var id = await mediator.Send(request, cancellationToken);
            return TypedResults.Created($"/api/v1/school/classes/{id}", id);
        })
        .WithName("CreateClasse")
        .WithSummary("Créer une nouvelle classe")
        .RequirePermission(SchoolManagementPermissionConstants.Classes.Create)
        .Produces<Guid>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }
}
