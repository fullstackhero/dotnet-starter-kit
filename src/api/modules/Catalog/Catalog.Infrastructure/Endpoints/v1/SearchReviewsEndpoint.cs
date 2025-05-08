using FSH.Framework.Core.Paging;
using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Reviews.Get.v1;
using FSH.Starter.WebApi.Catalog.Application.Reviews.Search.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class SearchReviewsEndpoint
{
    internal static RouteHandlerBuilder MapSearchReviewsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/Reviews/search", async (ISender mediator, [FromBody] SearchReviewsCommand command) =>
            {
                var response = await mediator.Send(command);
                return Results.Ok(response);
            })
            .WithName(nameof(SearchReviewsEndpoint))
            .WithSummary("Searches Reviews with pagination and filtering")
            .WithDescription("Searches Reviews with pagination and filtering")
            .Produces<PagedList<ReviewResponse>>()
            .RequirePermission("Permissions.Reviews.View")
            .MapToApiVersion(1);
    }
}
