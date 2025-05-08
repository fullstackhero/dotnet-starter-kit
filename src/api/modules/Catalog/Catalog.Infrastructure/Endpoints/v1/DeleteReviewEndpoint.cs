using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Reviews.Delete.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class DeleteReviewEndpoint
{
    internal static RouteHandlerBuilder MapReviewDeleteEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapDelete("/Reviews/{id:guid}", async (Guid id, ISender mediator) =>
            {
                await mediator.Send(new DeleteReviewCommand(id));
                return Results.NoContent();
            })
            .WithName(nameof(DeleteReviewEndpoint))
            .WithSummary("Deletes a Review by ID")
            .WithDescription("Deletes a Review by ID")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("Permissions.Reviews.Delete")
            .MapToApiVersion(1);
    }
}
