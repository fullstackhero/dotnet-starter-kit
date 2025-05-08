using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Cities.Create.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class CreateCityEndpoint
{
    internal static RouteHandlerBuilder MapCityCreationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/cities", async (CreateCityCommand request, ISender mediator) =>
            {
                var response = await mediator.Send(request);
                return Results.Ok(response);
            })
            .WithName(nameof(CreateCityEndpoint))
            .WithSummary("Creates a City")
            .WithDescription("Creates a City")
            .Produces<CreateCityResponse>()
            .RequirePermission("Permissions.Cities.Create")
            .MapToApiVersion(1);
    }
}
