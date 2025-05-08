using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Reviews.Create.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class CreateReviewEndpoint
{
    internal static RouteHandlerBuilder MapReviewCreationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/Reviews", async (CreateReviewCommand request, ISender mediator) =>
            {
                var response = await mediator.Send(request);
                return Results.Ok(response);
            })
            .WithName(nameof(CreateReviewEndpoint))
            .WithSummary("Creates a Review")
            .WithDescription("Creates a Review")
            .Produces<CreateReviewResponse>()
            .RequirePermission("Permissions.Reviews.Create")
            .MapToApiVersion(1);
    }
}
