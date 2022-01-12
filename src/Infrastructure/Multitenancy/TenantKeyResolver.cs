using System.Security.Claims;
using FSH.WebApi.Shared.Multitenancy;
using Microsoft.AspNetCore.Http;

namespace FSH.WebApi.Infrastructure.Multitenancy;

public static class TenantKeyResolver
{
    public static string? ResolveFrom(HttpContext context)
    {
        string? tenantKey = ResolveFromUserAuth(context);
        if (!string.IsNullOrEmpty(tenantKey))
        {
            return tenantKey;
        }

        tenantKey = ResolveFromHeader(context);
        if (!string.IsNullOrEmpty(tenantKey))
        {
            return tenantKey;
        }

        tenantKey = ResolveFromQuery(context);
        if (!string.IsNullOrEmpty(tenantKey))
        {
            return tenantKey;
        }

        return default;
    }

    private static string? ResolveFromUserAuth(HttpContext context) =>
        context.User.GetTenant();

    private static string? ResolveFromHeader(HttpContext context) =>
        context.Request.Headers.TryGetValue(MultitenancyConstants.TenantKeyName, out var tenantKey)
            ? (string)tenantKey
            : default;

    private static string? ResolveFromQuery(HttpContext context) =>
        context.Request.Query.TryGetValue(MultitenancyConstants.TenantKeyName, out var tenantKey)
            ? (string)tenantKey
            : default;
}