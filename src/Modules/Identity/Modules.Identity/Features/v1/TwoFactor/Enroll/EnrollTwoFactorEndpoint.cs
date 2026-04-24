using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.v1.TwoFactor;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.TwoFactor.Enroll;

public static class EnrollTwoFactorEndpoint
{
    internal static RouteHandlerBuilder MapEnrollTwoFactorEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/2fa/enroll",
                async (IMediator mediator, CancellationToken ct) =>
                    TypedResults.Ok(await mediator.Send(new EnrollTwoFactorCommand(), ct)))
            .WithName("EnrollTwoFactor")
            .WithSummary("Begin TOTP enrollment")
            .WithDescription("Generates (or rotates) the current user's authenticator shared secret and returns it plus an otpauth:// URI for QR rendering. 2FA is NOT enabled until the caller confirms with /2fa/verify.")
            .RequireAuthorization()
            .Produces<TwoFactorEnrollmentResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);
    }
}
