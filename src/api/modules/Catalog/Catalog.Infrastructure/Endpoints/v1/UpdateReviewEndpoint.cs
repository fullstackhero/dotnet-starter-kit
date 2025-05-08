using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Reviews.Update.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class UpdateReviewEndpoint
{
    internal static RouteHandlerBuilder MapReviewUpdateEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPut("/Reviews/{id:guid}", async (Guid id, UpdateReviewCommand request, ISender mediator) =>
            {
                if (id != request.Id) return Results.BadRequest();
                var response = await mediator.Send(request);
                return Results.Ok(response);
            })
            .WithName(nameof(UpdateReviewEndpoint))
            .WithSummary("Updates a Review")
            .WithDescription("Updates a Review")
            .Produces<UpdateReviewResponse>()
            .RequirePermission("Permissions.Reviews.Update")
            .MapToApiVersion(1);
    }
}
