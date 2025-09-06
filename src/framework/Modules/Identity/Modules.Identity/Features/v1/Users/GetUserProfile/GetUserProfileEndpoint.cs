﻿using FSH.Framework.Core.Exceptions;
using FSH.Framework.Identity.Core.Users;
using FSH.Framework.Shared.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;

namespace FSH.Framework.Infrastructure.Identity.Users.Endpoints;
public static class GetUserProfileEndpoint
{
    internal static RouteHandlerBuilder MapGetMeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/profile", async (ClaimsPrincipal user, IUserService service, CancellationToken cancellationToken) =>
        {
            if (user.GetUserId() is not { } userId || string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedException();
            }

            return await service.GetAsync(userId, cancellationToken);
        })
        .WithName("GetMeEndpoint")
        .WithSummary("Get current user information based on token")
        .WithDescription("Get current user information based on token");
    }
}