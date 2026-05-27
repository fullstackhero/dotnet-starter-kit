using FSH.Framework.Shared.Identity.Claims;
using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace FSH.Framework.Web.Observability.Logging.Serilog;

public class HttpRequestContextEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpRequestContextEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(propertyFactory);

        // Get HttpContext properties here
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext != null)
        {
            // Add properties to the log event based on HttpContext
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("RequestMethod", httpContext.Request.Method));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("RequestPath", httpContext.Request.Path));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserAgent", httpContext.Request.Headers["User-Agent"]));

            if (httpContext.User?.Identity?.IsAuthenticated == true)
            {
                var userId = httpContext.User.GetUserId();
                var tenant = httpContext.User.GetTenant();
                var userEmailId = httpContext.User.GetEmail();

                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserId", userId));
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Tenant", tenant));
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserEmail", userEmailId));
            }
        }
    }
}