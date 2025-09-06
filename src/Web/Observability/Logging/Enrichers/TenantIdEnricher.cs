using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;
using System.Security.Claims;

namespace FSH.Framework.Web.Observability.Logging.Enrichers;

public sealed class TenantIdEnricher : ILogEventEnricher
{
    public const string PropertyName = "TenantId";
    private static readonly HttpContextAccessor Accessor = new();

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(propertyFactory);

        var http = Accessor.HttpContext;
        string tenantId =
            GetFromClaims(http?.User) ??
            GetFromItems(http) ??
            GetFromHeader(http) ??
            "unknown";

        var prop = propertyFactory.CreateProperty(PropertyName, tenantId, false);
        logEvent.AddPropertyIfAbsent(prop);
    }

    private static string? GetFromClaims(ClaimsPrincipal? user) =>
        user?.FindFirstValue("tenant_id")
        ?? user?.FindFirstValue("tid")
        ?? user?.FindFirstValue("TenantId");

    private static string? GetFromItems(HttpContext? ctx) =>
        ctx is not null && ctx.Items.TryGetValue("TenantId", out var v) ? v?.ToString() : null;

    private static string? GetFromHeader(HttpContext? ctx) =>
        ctx?.Request?.Headers.TryGetValue("X-Tenant", out var val) == true ? val.ToString() : null;
}