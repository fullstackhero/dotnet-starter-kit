using FSH.Modules.Identity.Contracts.v1.Users.SetProfileImage;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Users.SetProfileImage;

public static class SetProfileImageEndpoint
{
    internal static RouteHandlerBuilder MapSetProfileImageEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPut("/profile/image",
                async (SetProfileImageCommand command, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("SetProfileImage")
            .WithSummary("Set the authenticated user's avatar URL")
            .WithDescription("Persists a durable image URL on the current user's profile. Typically called after the Files module's presigned-upload flow returns a publicUrl. Pass a null/empty body to clear.")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status400BadRequest);
}
