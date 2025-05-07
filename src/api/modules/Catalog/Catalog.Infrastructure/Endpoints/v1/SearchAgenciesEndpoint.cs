using FSH.Framework.Core.Paging;
using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Agencies.Get.v1;
using FSH.Starter.WebApi.Catalog.Application.Agencies.Search.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;

public static class SearchAgencysEndpoint
{
    internal static RouteHandlerBuilder MapGetAgencyListEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/search", async (ISender mediator, [FromBody] SearchAgenciesCommand command) =>
            {
                var response = await mediator.Send(command);
                return Results.Ok(response);
            })
            .WithName(nameof(SearchAgencysEndpoint))
            .WithSummary("Gets a list of Agencies")
            .WithDescription("Gets a list of Agencies with pagination and filtering support")
            .Produces<PagedList<AgencyResponse>>()
            .RequirePermission("Permissions.Agencies.View")
            .MapToApiVersion(1);
    }
}
