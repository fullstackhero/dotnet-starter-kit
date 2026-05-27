# Background jobs (Hangfire)

`src/BuildingBlocks/Jobs/`. Read before enqueuing or scheduling work.

## Fire-and-forget / scheduled — `IJobService`

Inject `IJobService` (`Jobs/Services/IJobService.cs`) and use it; don't call Hangfire's `BackgroundJob` directly in feature code.

```csharp
jobService.Enqueue(() => mailService.SendAsync(req, CancellationToken.None));   // default queue
jobService.Enqueue("email", () => mailService.SendAsync(req, CancellationToken.None));
jobService.Schedule(() => DoLater(), TimeSpan.FromMinutes(5));
```

Queues: `default`, `email` (5 workers, 30s poll). Storage auto-selected from `DatabaseOptions.Provider` (Postgres/MSSQL).

## Recurring jobs — `IRecurringJobManager`

`IJobService` has **no** recurring API. Register recurring jobs in the module's `MapEndpoints` with `IRecurringJobManager.AddOrUpdate<T>(...)`, always `TimeZoneInfo.Utc`:

```csharp
recurringJobs.AddOrUpdate<PurgeOrphanedFilesJob>("files:purge-orphaned",
    j => j.RunAsync(CancellationToken.None), Cron.Hourly(), new() { TimeZone = TimeZoneInfo.Utc });
```

Examples in the tree: `PurgeOrphanedFiles`/`PurgeDeletedFiles` (Files), `MonthlyInvoiceJob` (Billing), `AuditRetentionJob` (Auditing), `WebhookDispatchJob` (Webhooks).

## Dashboard & config

`/jobs` (default), behind `HangfireOptions.UserName`/`Password` basic auth — both `[Required]`, password `[MinLength(12)]`, so **startup fails in non-dev if unset**.

## Gotchas

- Jobs run on the server with **no HTTP/tenant context** — restore Finbuckle tenant context inside the job (fresh scope + `IMultiTenantContextSetter`) before touching a tenant-filtered DbContext.
- The DbMigrator registers `NoOpJobService` whose methods **throw** — surfaces any accidental enqueue during migration. Don't enqueue from migration/seed paths.
- A job class is a normal DI-resolved type (scope-per-job via `FshJobActivator`); inject what you need.
