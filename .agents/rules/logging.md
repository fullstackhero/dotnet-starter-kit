# Logging & observability

`src/BuildingBlocks/Web/Observability/`. Read before adding logs, traces, or metrics.

## Structured logging only

**No string interpolation in log messages.** Use message templates with named placeholders, or `[LoggerMessage]` source-gen for hot paths.

```csharp
// good
_logger.LogInformation("Cleaned up {Count} expired sessions for tenant {TenantId}", count, tenantId);
// also good (hot path) — see OutboxDispatcher, InMemoryEventBus, AppHub
[LoggerMessage(Level = LogLevel.Warning, Message = "Outbox message {MessageId} dead-lettered")]
private partial void LogDeadLettered(Guid messageId);
// NEVER
_logger.LogInformation($"Cleaned up {count} sessions");   // breaks structured logging + analyzers
```

Build runs with `TreatWarningsAsErrors` — interpolated log calls won't even compile clean under analysis.

## Serilog

`AddHeroLogging()` reads the `Serilog` config section (Console sink by default), attaches `HttpRequestContextEnricher` (adds `RequestMethod`/`RequestPath`/`UserAgent` + `UserId`/`Tenant`/`UserEmail` when authenticated), overrides Microsoft/EF/Hangfire/Finbuckle to higher levels, and excludes the `ExceptionHandlerMiddleware` source (the global handler logs exceptions itself — don't double-log).

## Correlation

`X-Correlation-ID` request header (falls back to `HttpContext.TraceIdentifier`), surfaced in every ProblemDetails and pushed to the Serilog `LogContext`. `CurrentUserMiddleware` tags the current `Activity` with `fsh.user_id` / `fsh.tenant_id` / `fsh.correlation_id`.

## OpenTelemetry

`AddHeroOpenTelemetry()` no-ops unless `OpenTelemetryOptions.Enabled`. Metrics + traces for AspNetCore/HttpClient/Npgsql/EFCore/Redis/Runtime, plus caching + auditing meters and Mediator pipeline spans (`MediatorTracingBehavior`). **OTLP exporter is off by default** (`Exporter.Otlp.Enabled=false`); endpoint `http://localhost:4317` (the Aspire/compose collector). Add a new meter/source name to `OpenTelemetryOptions` config, not by editing the extension.
