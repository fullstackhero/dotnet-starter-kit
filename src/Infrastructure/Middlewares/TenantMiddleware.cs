using DN.WebApi.Application.Abstractions.Services.General;
using DN.WebApi.Application.Abstractions.Services.Identity;
using DN.WebApi.Application.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace DN.WebApi.Infrastructure.Middlewares
{
    public class TenantMiddleware : IMiddleware
    {
        private readonly IStringLocalizer<TenantMiddleware> _localizer;
        private readonly ICurrentUser _currentUser;
        private readonly ITenantService _tenantService;

        public TenantMiddleware(IStringLocalizer<TenantMiddleware> localizer, ICurrentUser currentUser, ITenantService tenantService)
        {
            _localizer = localizer;
            _currentUser = currentUser;
            _tenantService = tenantService;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (_currentUser.IsAuthenticated())
            {
                _tenantService.SetCurrentTenant(_currentUser.GetTenant());
            }
            else
            {
                if (!ExcludePath(context) && !ResolveFromHeader(context) && !ResolveFromQuery(context))
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

        private bool ResolveFromHeader(HttpContext context)
        {
            context.Request.Headers.TryGetValue("tenant", out var tenantFromHeader);
            if (!string.IsNullOrEmpty(tenantFromHeader))
            {
                _tenantService.SetCurrentTenant(tenantFromHeader);
                return true;
            }

            return false;
        }

        private bool ResolveFromQuery(HttpContext context)
        {
            context.Request.Query.TryGetValue("tenant", out var tenantFromQueryString);
            if (!string.IsNullOrEmpty(tenantFromQueryString))
            {
                _tenantService.SetCurrentTenant(tenantFromQueryString);
                return true;
            }

            return false;
        }
    }
}