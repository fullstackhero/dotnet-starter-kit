﻿using System.Security.Claims;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Core.Identity.Users.Abstractions;
using FSH.Framework.Infrastructure.Auth.Policy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Infrastructure.Identity.Users.Endpoints;
public static class DeleteUserEndpoint
{
    internal static RouteHandlerBuilder MapDeleteUserEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/{userId}", (string userId, ClaimsPrincipal user, IUserService service) =>
        {
            if (user.GetUserId() is not { } loggedInUserId || string.IsNullOrEmpty(loggedInUserId))
            {
                throw new UnauthorizedException();
            }

            return service.DeleteAsync(userId);
        })
        .WithName(nameof(DeleteUserEndpoint))
        .WithSummary("delete user profile")
        .RequirePermission("Permissions.Users.Delete")
        .WithDescription("delete user profile");
    }
}
