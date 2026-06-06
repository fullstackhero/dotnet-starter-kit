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

`AddHeroOpenTelemetry()` no-ops unless `OpenTelemetryOptions.Enabled`. Metrics + traces for AspNetCore/HttpClient/Npgsql/EFCore/Redis/Runtime, plus caching + auditing meters and Mediator pipeline spans (`MediatorTracingBehavior`). Add a new meter/source name to `OpenTelemetryOptions` config, not by editing the extension.

**OTLP export is auto-detected.** It turns on when **either** `Exporter.Otlp.Enabled=true` **or** the `OTEL_EXPORTER_OTLP_ENDPOINT` env var is present. Under .NET Aspire that env var is injected automatically, so traces/metrics flow to the Aspire dashboard with no config change (the SDK reads endpoint + protocol from the standard `OTEL_EXPORTER_OTLP_*` env vars; the config `Endpoint`/`Protocol` are only used when no env var is set — e.g. the docker-compose collector at `http://localhost:4317`). Plain `dotnet run` with no collector and `Enabled=false` exports nothing.

**Logs ride the same OTLP detection.** Serilog owns the logging pipeline and does not forward to other `ILogger` providers, so the OTel SDK's log exporter can't see Serilog events — instead `AddHeroLogging` adds a `Serilog.Sinks.OpenTelemetry` sink under the same auto-detect rule (global `Enabled` + env-var-or-config endpoint). It also parses `OTEL_EXPORTER_OTLP_HEADERS` (the Aspire dashboard's OTLP receiver requires its `x-otlp-api-key`; the SDK reads this for traces/metrics, the Serilog sink does not) and stamps `service.name` = `OTEL_SERVICE_NAME ?? ApplicationName` so logs group under the same dashboard resource as the spans. Both `AddHeroOpenTelemetry` and the Serilog sink resolve the name that way: under an orchestrator that injects `OTEL_SERVICE_NAME` (Aspire sets it to the resource name, e.g. `fsh-starter-api`) the process adopts that identity; hardcoding the entry-assembly name (`FSH.Starter.Api`) instead would de-correlate the telemetry and list the process twice in the dashboard. Plain `dotnet run` with no env var falls back to `ApplicationName`.
