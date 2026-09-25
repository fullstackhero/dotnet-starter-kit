using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Frontend;
using FSH.Modules.Identity.Contracts.Authorization;
using FSH.Modules.Identity.Contracts.v1.Users.ResendConfirmationEmail;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Users.ResendConfirmationEmail;

public static class ResendConfirmationEmailEndpoint
{
    internal static RouteHandlerBuilder MapResendConfirmationEmailEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/users/{id:guid}/resend-confirmation-email", Handler)
        .WithName("ResendConfirmationEmail")
        .WithSummary("Resend a user's email confirmation (admin)")
        .RequirePermission(IdentityPermissions.Users.ConfirmEmail)
        .WithDescription("Re-sends the email-confirmation link to a user who has not confirmed their address yet.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<NoContent> Handler(
        Guid id,
        IFrontendOriginResolver originResolver,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        // Operator-driven flow: an admin re-sends a tenant user's confirmation, so the link must
        // land on the recipient's app (the default front-end), not the operator's Origin.
        var origin = originResolver.ResolveDefault();
        await mediator.Send(new ResendConfirmationEmailCommand(id.ToString(), origin), cancellationToken);
        return TypedResults.NoContent();
    }
}
