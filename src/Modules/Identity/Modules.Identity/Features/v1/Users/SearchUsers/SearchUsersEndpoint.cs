using FSH.Framework.Shared.Persistence;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Framework.Shared.Identity;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Identity.Contracts.v1.Users.SearchUsers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Users.SearchUsers;

public static class SearchUsersEndpoint
{
    internal static RouteHandlerBuilder MapSearchUsersEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet(
                "/users/search",
                async ([AsParameters] SearchUsersQuery query, IMediator mediator, CancellationToken cancellationToken) =>
                    TypedResults.Ok(await mediator.Send(query, cancellationToken)))
            .WithName("SearchUsers")
            .WithSummary("Search users with pagination")
            .WithDescription("Search and filter users with server-side pagination, sorting, and filtering by status, email confirmation, and role.")
            .RequirePermission(IdentityPermissionConstants.Users.View)
            .Produces<PagedResponse<UserDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }
}
