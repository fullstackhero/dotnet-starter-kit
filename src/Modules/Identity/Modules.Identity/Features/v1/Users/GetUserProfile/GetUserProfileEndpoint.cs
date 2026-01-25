using System.Security.Claims;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Identity.Claims;
using FSH.Modules.Identity.Contracts.v1.Users.GetUserProfile;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Users.GetUserProfile;

public static class GetUserProfileEndpoint
{
    internal static RouteHandlerBuilder MapGetMeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/profile", async (ClaimsPrincipal user, IMediator mediator, CancellationToken cancellationToken) =>
        {
            if (user.GetUserId() is not { } userId || string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedException();
            }

            return await mediator.Send(new GetCurrentUserProfileQuery(userId), cancellationToken);
        })
        .WithName("GetCurrentUserProfile")
        .WithSummary("Get current user profile")
        .WithDescription("Retrieve the authenticated user's profile from the access token.")
        .RequireAuthorization();
    }
}
