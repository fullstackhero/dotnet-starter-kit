using FSH.Framework.Core.Paging;
using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.PropertyTypes.Get.v1;
using FSH.Starter.WebApi.Catalog.Application.PropertyTypes.Search.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class SearchPropertyTypesEndpoint
{
    internal static RouteHandlerBuilder MapSearchPropertyTypesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/propertytypes/search", async (ISender mediator, [FromBody] SearchPropertyTypesCommand command) =>
            {
                var response = await mediator.Send(command);
                return Results.Ok(response);
            })
            .WithName(nameof(SearchPropertyTypesEndpoint))
            .WithSummary("Searches PropertyTypes with pagination and filtering")
            .WithDescription("Searches PropertyTypes with pagination and filtering")
            .Produces<PagedList<PropertyTypeResponse>>()
            .RequirePermission("Permissions.PropertyTypes.View")
            .MapToApiVersion(1);
    }
}
