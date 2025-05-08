using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Cities.Update.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class UpdateCityEndpoint
{
    internal static RouteHandlerBuilder MapCityUpdateEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPut("/cities/{id:guid}", async (Guid id, UpdateCityCommand request, ISender mediator) =>
            {
                if (id != request.Id) return Results.BadRequest();
                var response = await mediator.Send(request);
                return Results.Ok(response);
            })
            .WithName(nameof(UpdateCityEndpoint))
            .WithSummary("Updates a City")
            .WithDescription("Updates a City")
            .Produces<UpdateCityResponse>()
            .RequirePermission("Permissions.Cities.Update")
            .MapToApiVersion(1);
    }
}
