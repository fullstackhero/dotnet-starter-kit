using DN.WebApi.Shared.Multitenancy;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace DN.WebApi.Infrastructure.Multitenancy;

public static class TenantResolver
{
    public static string? Resolver(HttpContext context)
    {
        string? tenantId = ResolveFromUserAuth(context);
        if (!string.IsNullOrEmpty(tenantId))
        {
            return tenantId;
        }

        tenantId = ResolveFromHeader(context);
        if (!string.IsNullOrEmpty(tenantId))
        {
            return tenantId;
        }

        tenantId = ResolveFromQuery(context);
        if (!string.IsNullOrEmpty(tenantId))
        {
            return tenantId;
        }

        return default;
    }

    private static string? ResolveFromUserAuth(HttpContext context) =>
        context.User.GetTenant();

    private static string? ResolveFromHeader(HttpContext context) =>
        context.Request.Headers.TryGetValue(MultitenancyConstants.TenantKeyName, out var tenantFromHeader)
            ? (string)tenantFromHeader
            : default;

    private static string? ResolveFromQuery(HttpContext context) =>
        context.Request.Query.TryGetValue(MultitenancyConstants.TenantKeyName, out var tenantFromQueryString)
            ? (string)tenantFromQueryString
            : default;
}