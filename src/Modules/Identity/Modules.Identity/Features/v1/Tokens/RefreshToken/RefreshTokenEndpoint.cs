using FSH.Modules.Identity.Contracts.v1.Tokens.RefreshToken;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Tokens.RefreshToken;

public static class RefreshTokenEndpoint
{
    public static RouteHandlerBuilder MapRefreshTokenEndpoint(this IEndpointRouteBuilder endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        return endpoint.MapPost("/token/refresh",
            [AllowAnonymous] async Task<Results<Ok<RefreshTokenCommandResponse>, UnauthorizedHttpResult, ProblemHttpResult>>
            ([FromBody] RefreshTokenCommand command,
            [FromHeader(Name = "tenant")] string tenant,
            [FromServices] IMediator mediator,
            CancellationToken ct) =>
            {
                var response = await mediator.Send(command, ct);
                return TypedResults.Ok(response);
            })
            .WithName("RefreshJwtTokens")
            .WithSummary("Refresh JWT access and refresh tokens")
            .WithDescription("Use a valid (possibly expired) access token together with a valid refresh token to obtain a new access token and a rotated refresh token.")
            .Produces<RefreshTokenCommandResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}