using DN.WebApi.Application.Abstractions.Services.General;
using DN.WebApi.Application.Abstractions.Services.Identity;
using DN.WebApi.Application.Exceptions;
using DN.WebApi.Infrastructure.Identity.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DN.WebApi.Infrastructure.Middlewares
{
    public class TenantMiddleware : IMiddleware
    {
        private readonly IStringLocalizer<TenantMiddleware> _localizer;

        public TenantMiddleware(IStringLocalizer<TenantMiddleware> localizer)
        {
            _localizer = localizer;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var userInfo = context.RequestServices.GetRequiredService<ICurrentUser>();
            userInfo.SetUser(context.User);
            var tenantInfo = context.RequestServices.GetRequiredService<ITenantService>();
            if (userInfo.IsAuthenticated())
            {
                tenantInfo.SetTenant(userInfo.GetTenant());
            }
            else
            {
                string tenantFromQueryString = System.Web.HttpUtility.ParseQueryString(context.Request.QueryString.Value).Get("tenant");
                if (!string.IsNullOrEmpty(tenantFromQueryString))
                {
                    tenantInfo.SetTenant(tenantFromQueryString);
                }
                else if (context.Request.Headers.TryGetValue("tenant", out var tenant))
                {
                    tenantInfo.SetTenant(tenant);
                }
                else
                {
                    throw new IdentityException(_localizer["auth.failed"], statusCode: HttpStatusCode.Unauthorized);
                }
            }

            await next(context);
        }
    }
}