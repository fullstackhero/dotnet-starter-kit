using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Reviews.Get.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class GetReviewEndpoint
{
    internal static RouteHandlerBuilder MapGetReviewEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet("/Reviews/{id:guid}", async (Guid id, ISender mediator) =>
            {
                var response = await mediator.Send(new GetReviewRequest(id));
                return Results.Ok(response);
            })
            .WithName(nameof(GetReviewEndpoint))
            .WithSummary("Gets a Review by ID")
            .WithDescription("Gets a Review by ID")
            .Produces<ReviewResponse>()
            .RequirePermission("Permissions.Reviews.View")
            .MapToApiVersion(1);
    }
}
