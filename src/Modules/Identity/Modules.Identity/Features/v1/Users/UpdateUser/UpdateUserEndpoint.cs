using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Identity.Claims;
using FSH.Modules.Identity.Contracts.v1.Users.UpdateUser;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;
using System.Security.Claims;

namespace FSH.Modules.Identity.Features.v1.Users.UpdateUser;

public static class UpdateUserEndpoint
{
    internal static RouteHandlerBuilder MapUpdateUserEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/profile", async ([FromBody] UpdateUserCommand request, ClaimsPrincipal user, HttpRequest httpRequest, IMediator mediator, CancellationToken cancellationToken) =>
        {
            if (user.GetUserId() is not { } userId || string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedException();
            }

            // Force the target id to the authenticated user — this endpoint is for self-update
            // only, regardless of any id the caller supplied in the body.
            request.Id = userId;

            // Header-derived, so it overwrites whatever the body carried.
            request.ExpectedConcurrencyStamps = ReadExpectedConcurrencyStamps(httpRequest);

            await mediator.Send(request, cancellationToken);
            return TypedResults.Ok();
        })
        .WithName("UpdateUserProfile")
        .WithSummary("Update user profile")
        .RequireAuthorization()
        .WithDescription("Update profile details for the authenticated user. Any signed-in user may edit their own profile; no admin permission required. Echo the ETag from GET /identity/profile in If-Match and a stale full-representation update is rejected with 412 instead of silently overwriting a concurrent change.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status412PreconditionFailed);
    }

    /// <summary>
    /// Turns the request's <c>If-Match</c> header into the set of concurrency tokens the caller is
    /// willing to overwrite. Returns <see langword="null"/> when there is no precondition to
    /// enforce: either the header is absent, or it is <c>*</c>, which asks only that the resource
    /// exist — and it does, or the update answers 404 on its own.
    /// </summary>
    private static List<string>? ReadExpectedConcurrencyStamps(HttpRequest request)
    {
        var ifMatch = request.Headers.IfMatch;
        if (ifMatch.Count == 0)
        {
            return null;
        }

        if (!EntityTagHeaderValue.TryParseStrictList(ifMatch, out var entityTags))
        {
            // Answering 412 would send a well-behaved client into a refetch-and-retry loop it can
            // never win, since the malformed header is its own bug. 400 names the bug instead.
            throw new BadHttpRequestException("The If-Match header is not a valid entity-tag list.");
        }

        if (entityTags.Contains(EntityTagHeaderValue.Any))
        {
            return null;
        }

        // If-Match mandates the strong comparison function, so a weak validator can never match.
        // Dropping the weak entries leaves a list no stored token matches, which is exactly the
        // 412 the RFC asks for.
        return entityTags
            .Where(entityTag => !entityTag.IsWeak)
            .Select(entityTag => entityTag.Tag.ToString().Trim('"'))
            .ToList();
    }
}