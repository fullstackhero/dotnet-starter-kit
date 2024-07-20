using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FSH.Framework.Core.Identity.Users.Abstractions;
using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Framework.Infrastructure.Identity.Users.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FSH.Framework.Infrastructure.HealthChecks;
public static class HealthCheckEndpoint
{
    internal static RouteHandlerBuilder MapCustomHealthCheckEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/", (HttpContext context) =>
        {
            var healthCheckService = context.RequestServices.GetRequiredService<HealthCheckService>();
            var report = healthCheckService.CheckHealthAsync().Result;
            
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
