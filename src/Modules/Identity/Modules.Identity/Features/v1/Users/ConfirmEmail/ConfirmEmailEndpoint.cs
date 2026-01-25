using FSH.Modules.Identity.Contracts.v1.Users.ConfirmEmail;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Users.ConfirmEmail;

public static class ConfirmEmailEndpoint
{
    internal static RouteHandlerBuilder MapConfirmEmailEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/confirm-email", async (string userId, string code, string tenant, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new ConfirmEmailCommand(userId, code, tenant), cancellationToken);
            return TypedResults.Ok(result);
        })
        .WithName("ConfirmEmail")
        .WithSummary("Confirm user email")
        .WithDescription("Confirm a user's email address.")
        .AllowAnonymous();
    }
}
