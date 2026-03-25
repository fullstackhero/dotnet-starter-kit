using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.SchoolManagement.Contracts.v1.AnneeScolaires.CreateAnneeScolaire;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.SchoolManagement.Features.v1.AnneeScolaires.CreateAnneeScolaire;

public static class CreateAnneeScolaireEndpoint
{
    public static RouteHandlerBuilder MapCreateAnneeScolaireEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/annees-scolaires", async (IMediator mediator, [FromBody] CreateAnneeScolaireCommand request, CancellationToken cancellationToken) =>
        {
            var id = await mediator.Send(request, cancellationToken);
            return TypedResults.Created($"/api/v1/school/annees-scolaires/{id}", id);
        })
        .WithName("CreateAnneeScolaire")
        .WithSummary("Create a new school year")
        .RequirePermission(SchoolManagementPermissionConstants.AnneeScolaires.Create)
        .Produces<Guid>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }
}
