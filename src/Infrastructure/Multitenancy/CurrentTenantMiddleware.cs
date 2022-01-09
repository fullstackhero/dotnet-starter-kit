using System.Net;
using DN.WebApi.Application.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;

namespace DN.WebApi.Infrastructure.Multitenancy;

public class CurrentTenantMiddleware : IMiddleware
{
    private readonly IStringLocalizer<CurrentTenantMiddleware> _localizer;
    private readonly ICurrentTenantInitializer _currentTenantInitializer;

    public CurrentTenantMiddleware(IStringLocalizer<CurrentTenantMiddleware> localizer, ICurrentTenantInitializer currentTenantInitializer)
    {
        _localizer = localizer;
        _currentTenantInitializer = currentTenantInitializer;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (!ExcludePath(context))
        {
            string? tenantKey = TenantKeyResolver.ResolveFrom(context);
            if (!string.IsNullOrEmpty(tenantKey))
            {
                _currentTenantInitializer.SetCurrentTenant(tenantKey);
            }
            else
            {
                throw new IdentityException(_localizer["auth.failed"], statusCode: HttpStatusCode.Unauthorized);
            }
        }

        await next(context);
    }

    private bool ExcludePath(HttpContext context)
    {
        var listExclude = new List<string>()
            {
                "/swagger",
                "/jobs"
            };

        foreach (string item in listExclude)
        {
            if (context.Request.Path.StartsWithSegments(item))
            {
                return true;
            }
        }

        return false;
    }
}