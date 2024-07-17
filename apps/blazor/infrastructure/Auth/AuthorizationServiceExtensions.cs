﻿using FSH.Blazor.Shared;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace FSH.Blazor.Infrastructure.Auth;

public static class AuthorizationServiceExtensions
{
    public static async Task<bool> HasPermissionAsync(this IAuthorizationService service, ClaimsPrincipal user, string action, string resource)
    {
        return (await service.AuthorizeAsync(user, null, FshPermission.NameFor(action, resource))).Succeeded;
    }
}