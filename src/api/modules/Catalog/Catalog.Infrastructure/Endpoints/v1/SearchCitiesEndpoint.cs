using FSH.Framework.Core.Paging;
using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Cities.Get.v1;
using FSH.Starter.WebApi.Catalog.Application.Cities.Search.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class SearchCitiesEndpoint
{
    internal static RouteHandlerBuilder MapSearchCitiesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/cities/search", async (ISender mediator, [FromBody] SearchCitiesCommand command) =>
            {
                var response = await mediator.Send(command);
                return Results.Ok(response);
            })
            .WithName(nameof(SearchCitiesEndpoint))
            .WithSummary("Searches Cities with pagination and filtering")
            .WithDescription("Searches Cities with pagination and filtering")
            .Produces<PagedList<CityResponse>>()
            .RequirePermission("Permissions.Cities.View")
            .MapToApiVersion(1);
    }
}
