# Module: Auditing

Append-only audit trail (entity changes, security events, exceptions, HTTP activity) with async channel-buffered persistence + DLQ. Module `Order = 300`.

**Entities / DbContext:** `AuditRecord`, `AuditDbContext`. `AuditEnvelope` is the in-flight event. Rich Contracts surface: `IAuditClient`, `ISecurityAudit`, `IAuditPublisher`, `IAuditSink`, `IAuditDlqSink`, `IAuditEnricher`, `NoAuditAttribute`, payload records.
**Areas:** read-only query side — GetAudits / ByCorrelation / ByTrace / Summary / Exception / Security. Full list: `Features/v1/` or `/scalar`.

## Gotchas

- **Static `Audit` fluent API** — `Audit.ForSecurity(...).WithUser(...).WriteAsync(ct)` (also `ForEntityChange`/`ForActivity`/`ForException`). Configured once at startup via `Audit.Configure(publisher, serializer, enrichers)`. Enrichers are held in a **volatile immutable array swapped atomically** — never mutate a live enricher list (it'd race the enrich loop).
- **Two interceptors, don't confuse them:** `AuditingSaveChangesInterceptor` (this module) captures EF entity diffs → EntityChange events and **skips `AuditDbContext`** (no recursive self-audit). `AuditableEntitySaveChangesInterceptor` (BuildingBlocks) stamps audit/soft-delete fields — different file, different job.
- **Channel-buffered, never blocks the request** — `ChannelAuditPublisher` has two lanes: default (`DropOldest` under pressure) and a **security lane that back-pressures and never drops** (login/permission/impersonation ride here). `AuditBackgroundWorker` drains both (security first), batches, writes via `IAuditSink`; on sink failure it retries then spills to `IAuditDlqSink` (file) so events survive a Postgres outage.
- `SqlAuditSink` groups a batch by `TenantId` and sets tenant context per group in a fresh scope (null → Root) — background writer has no ambient tenant.
- **JSON masking** redacts fields by keyword (password/secret/token/apiKey/connectionString…) → `****`. Add sensitive keys there.
- Exclude an endpoint from activity auditing with `[NoAudit]` / the `NoAudit` endpoint extension.
