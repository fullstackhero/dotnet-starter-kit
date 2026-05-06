using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FSH.Framework.Web.Health;

public static class HealthEndpoints
{
    public sealed record HealthResult(string Status, IEnumerable<HealthEntry> Results);
    public sealed record HealthEntry(string Name, string Status, string? Description, double DurationMs, Dictionary<string, object>? Details = default);
    public static IEndpointRouteBuilder MapHeroHealthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/health")
                       .WithTags("Health")
                       .AllowAnonymous()
                       .DisableRateLimiting();


        // Liveness: only process up (no external deps)
        group.MapGet("/live",
                async Task<Ok<HealthResult>> (HealthCheckService hc, CancellationToken cancellationToken) =>
                {
                    var report = await hc.CheckHealthAsync(_ => false, cancellationToken);
                    var payload = new HealthResult(
                    Status: report.Status.ToString(),
                    Results: Array.Empty<HealthEntry>());

                    return TypedResults.Ok(payload);
                })
                .WithName("Liveness")
                .WithSummary("Quick process liveness probe.")
                .WithDescription("Reports if the API process is alive. Does not check dependencies.")
                .Produces<HealthResult>(StatusCodes.Status200OK);

        // Readiness: includes DB (and any other registered checks). Returns the
        // full payload on both 200 and 503 so dashboards/operators can see *which*
        // check failed and why. Probe consumers (k8s, load balancers) only key
        // off the status code, so adding a body on 503 is safe.
        group.MapGet("/ready",
                    async (HealthCheckService hc, CancellationToken cancellationToken) =>
                    {
                        var report = await hc.CheckHealthAsync(cancellationToken: cancellationToken);
                        var results = report.Entries.Select(e =>
                    new HealthEntry(
                        Name: e.Key,
                        Status: e.Value.Status.ToString(),
                        Description: e.Value.Description,
                        DurationMs: e.Value.Duration.TotalMilliseconds,
                        Details: e.Value.Data.ToDictionary(
                            k => k.Key,
                            v => v.Value is null ? "null" : v.Value
                        )));

                        var payload = new HealthResult(report.Status.ToString(), results);
                        var statusCode = report.Status == HealthStatus.Healthy
                            ? StatusCodes.Status200OK
                            : StatusCodes.Status503ServiceUnavailable;

                        return Results.Json(payload, statusCode: statusCode);
                    })
                    .WithName("Readiness")
                    .WithSummary("Readiness probe with database check.")
                    .WithDescription("Returns 200 if all dependencies are healthy, otherwise 503. Body is the same shape in both cases.")
                    .Produces<HealthResult>(StatusCodes.Status200OK)
                    .Produces<HealthResult>(StatusCodes.Status503ServiceUnavailable);

        return app;
    }
}