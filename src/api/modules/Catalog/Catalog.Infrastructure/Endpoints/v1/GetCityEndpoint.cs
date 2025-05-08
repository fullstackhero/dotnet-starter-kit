using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Cities.Get.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class GetCityEndpoint
{
    internal static RouteHandlerBuilder MapGetCityEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet("/cities/{id:guid}", async (Guid id, ISender mediator) =>
            {
                var response = await mediator.Send(new GetCityRequest(id));
                return Results.Ok(response);
            })
            .WithName(nameof(GetCityEndpoint))
            .WithSummary("Gets a City by ID")
            .WithDescription("Gets a City by ID")
            .Produces<CityResponse>()
            .RequirePermission("Permissions.Cities.View")
            .MapToApiVersion(1);
    }
}
