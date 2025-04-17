using FSH.Framework.Core.Messaging.CQRS;
using FSH.Framework.Identity.Contracts.v1.Tokens.TokenGeneration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.ComponentModel;

namespace FSH.Framework.Identity.v1.Tokens.TokenGeneration;
public static class TokenGenerationEndpoint
{
    internal static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/tokens", async (
            [FromBody] TokenGenerationCommand command,
            [DefaultValue("root")] string tenant,
            [FromServices] ICommandDispatcher dispatcher,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var result = await dispatcher.SendAsync(command, cancellationToken);
            return TypedResults.Ok(result);
        })
        .WithName(nameof(TokenGenerationEndpoint))
        .WithSummary("Generate JWTs")
        .WithDescription("Generates access and refresh tokens.")
        .AllowAnonymous();
    }
}