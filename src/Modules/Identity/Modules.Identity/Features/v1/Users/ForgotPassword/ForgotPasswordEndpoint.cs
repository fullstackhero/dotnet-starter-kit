using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Contracts.v1.Users.ForgotPassword;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Users.ForgotPassword;

public static class ForgotPasswordEndpoint
{
    internal static RouteHandlerBuilder MapForgotPasswordEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/forgot-password", async (
            HttpRequest request,
            [FromHeader(Name = MultitenancyConstants.Identifier)] string tenant,
            [FromBody] ForgotPasswordCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command, cancellationToken);
            return TypedResults.Ok(result);
        })
        .WithName("RequestPasswordReset")
        .WithSummary("Request password reset")
        .WithDescription("Generate a password reset token and send it via email.")
        .AllowAnonymous();
    }
}
