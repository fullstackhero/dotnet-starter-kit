using FSH.Framework.Core.Messaging.CQRS;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Identity.Endpoints.v1.Tokens.Generate;
public static class TokenGenerationEndpoint
{
    internal static RouteHandlerBuilder MapTokenGenerationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/", async (
            TokenGenerationCommand command,
            string tenant,
            ICommandDispatcher dispatcher,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var result = await dispatcher.SendAsync<TokenGenerationCommand, TokenGenerationResponse>(command, cancellationToken);
            return TypedResults.Ok(result);
        })
        .WithName("TokenGeneration")
        .WithSummary("Generate JWTs")
        .WithDescription("Generates access and refresh tokens.")
        .AllowAnonymous();
    }
}
