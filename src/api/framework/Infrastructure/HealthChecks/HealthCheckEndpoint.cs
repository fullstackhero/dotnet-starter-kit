using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FSH.Framework.Infrastructure.HealthChecks;
public static class HealthCheckEndpoint
{
    internal static RouteHandlerBuilder MapCustomHealthCheckEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/", async (HttpContext context) =>
        {
            var healthCheckService = context.RequestServices.GetRequiredService<HealthCheckService>();
            var report = await healthCheckService.CheckHealthAsync();
            var response = new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(entry => new
                {
                    name = entry.Key,
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description
                }),
                duration = report.TotalDuration
            };
            context.Response.ContentType = "application/json";
            return JsonSerializer.Serialize(response);
        })
        .WithName("HealthCheck")
        .WithSummary("Checks the health status of the application")
        .WithDescription("Provides detailed health information about the application.")
        .AllowAnonymous();
    }
}
