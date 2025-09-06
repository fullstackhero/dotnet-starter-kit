﻿using FSH.Framework.Identity.Core.Users;
using FSH.Framework.Shared.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Infrastructure.Identity.Users.Endpoints;
public static class GetUsersListEndpoint
{
    internal static RouteHandlerBuilder MapGetUsersListEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/", (CancellationToken cancellationToken, IUserService service) =>
        {
            return service.GetListAsync(cancellationToken);
        })
        .WithName(nameof(GetUsersListEndpoint))
        .WithSummary("get users list")
        .RequirePermission("Permissions.Users.View")
        .WithDescription("get users list");
    }
}