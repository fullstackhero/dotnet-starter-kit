using FSH.Framework.Core.Context;
using FSH.Framework.Shared.Identity.Claims;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace FSH.Framework.Web.Auth;

public class CurrentUserMiddleware(ICurrentUserInitializer currentUserInitializer) : IMiddleware
{
    private readonly ICurrentUserInitializer _currentUserInitializer = currentUserInitializer;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        _currentUserInitializer.SetCurrentUser(context.User);

        var activity = Activity.Current;
        if (activity is not null && context.User?.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.GetUserId();
            var tenant = context.User.GetTenant();
            var correlationId = context.Request.HttpContext.TraceIdentifier;

            if (!string.IsNullOrEmpty(userId))
                activity.SetTag("fsh.user_id", userId);

            if (!string.IsNullOrEmpty(tenant))
                activity.SetTag("fsh.tenant_id", tenant);

            if (!string.IsNullOrEmpty(correlationId))
                activity.SetTag("fsh.correlation_id", correlationId);
        }

        await next(context);
    }
}