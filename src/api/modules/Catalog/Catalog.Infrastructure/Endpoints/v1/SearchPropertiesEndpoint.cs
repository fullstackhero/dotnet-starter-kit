using FSH.Framework.Core.Paging;
using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Properties.Get.v1;
using FSH.Starter.WebApi.Catalog.Application.Properties.Search.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class SearchPropertiesEndpoint
{
    internal static RouteHandlerBuilder MapSearchPropertiesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet("/properties", async ([AsParameters] SearchPropertiesCommand command, ISender mediator) =>
            {
                var response = await mediator.Send(command);
                return Results.Ok(response);
            })
            .WithName(nameof(SearchPropertiesEndpoint))
            .WithSummary("Searches Properties")
            .WithDescription("Searches Properties")
            .Produces<PagedList<PropertyResponse>>()
            .RequirePermission("Permissions.Properties.View")
            .MapToApiVersion(1);
    }
}