using FSH.Modules.Identity.Contracts.v1.TwoFactor;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.TwoFactor.Disable;

public static class DisableTwoFactorEndpoint
{
    internal static RouteHandlerBuilder MapDisableTwoFactorEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/2fa/disable",
                async (DisableTwoFactorCommand command, IMediator mediator, CancellationToken ct) =>
                    TypedResults.Ok(new { success = await mediator.Send(command, ct) }))
            .WithName("DisableTwoFactor")
            .WithSummary("Disable TOTP for the current user")
            .WithDescription("Turns off 2FA after confirming the current password. Also rotates the authenticator secret so a re-enroll starts fresh.")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);
    }
}
