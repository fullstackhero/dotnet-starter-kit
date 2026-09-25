using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Identity.Claims;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.v1.Users.GetUserProfile;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;
using System.Security.Claims;

namespace FSH.Modules.Identity.Features.v1.Users.GetUserProfile;

public static class GetUserProfileEndpoint
{
    internal static RouteHandlerBuilder MapGetMeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/profile", async (ClaimsPrincipal user, HttpResponse response, IMediator mediator, CancellationToken cancellationToken) =>
        {
            if (user.GetUserId() is not { } userId || string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedException();
            }

            var profile = await mediator.Send(new GetCurrentUserProfileQuery(userId), cancellationToken);

            // The profile is a full-representation resource: PUT /profile rewrites every field, so
            // a caller editing a stale copy would blank whatever changed meanwhile. Publishing the
            // stored concurrency token as a strong ETag lets that caller echo it back in If-Match
            // and have the server reject the stale write.
            if (!string.IsNullOrEmpty(profile.ConcurrencyStamp))
            {
                response.Headers.ETag = new EntityTagHeaderValue($"\"{profile.ConcurrencyStamp}\"", isWeak: false).ToString();
            }

            return TypedResults.Ok(profile);
        })
        .WithName("GetCurrentUserProfile")
        .WithSummary("Get current user profile")
        .WithDescription("Retrieve the authenticated user's profile from the access token. The response carries a strong ETag — echo it in If-Match on PUT /identity/profile to reject a lost update.")
        .RequireAuthorization()
        .Produces<UserDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);
    }
}