# Database & EF Core conventions

Read before touching entities, DbContexts, migrations, or query filters.

## Entities

- `BaseEntity` — `Id`, `CreatedAt`, `UpdatedAt`, `TenantId`.
- `AggregateRoot` — `BaseEntity` + domain events (`IHasDomainEvents`, `_domainEvents` list).
- Marker interfaces: `IHasTenant`, `IAuditableEntity`, `ISoftDeletable`, `IGlobalEntity`.
- Domain events inherit `DomainEvent` (record: `EventId`, `OccurredOnUtc`, `CorrelationId`, `TenantId`). Integration events implement `IIntegrationEvent`; handlers `IIntegrationEventHandler<T>`.

## Tenant isolation (default-ON)

- `BaseDbContext` auto-applies a tenant query filter to every entity. **Isolation is on by default.**
- Opt out **only** via `IGlobalEntity` (e.g. `BillingPlan`, `ImpersonationGrant`, `Outbox`/`InboxMessage`).
- A subclass DbContext that overrides `OnModelCreating` **must call `base.OnModelCreating(modelBuilder)` LAST**, or the auto-applied filters are lost.
- Cross-tenant reads use `IgnoreQueryFilters()` **plus an explicit re-filter** — never rely on the absence of the filter.
- **Query-filter naming:** SoftDelete filter is *named*; the tenant filter stays *anonymous* (Finbuckle owns it). Don't rename the tenant filter.

## AsNoTracking — and when NOT to

- Read-only queries: add `.AsNoTracking()` (Specifications default to it).
- **Do NOT add `AsNoTracking()` to a read-then-mutate-then-`SaveChanges` query** — the entity must stay tracked or your changes won't persist. The analyzer (AP010) flags these as a smell, but for mutate-and-save flows it is a false positive — leave them tracked.
- `AnyAsync(...)` materializes no entity, so `AsNoTracking()` there is a no-op — skip it.

## Value generation for nav-collection children

A child entity reached **only** through a parent's navigation collection needs `Property(x => x.Id).ValueGeneratedNever()` in its EF config — otherwise EF treats it as `Modified` instead of `Added` and the insert silently misbehaves.

## Migrations

All migrations live in **one** project, `src/Host/FSH.Starter.Migrations.PostgreSQL`, organized **per-module by folder** (`Identity/`, `Catalog/`, `Chat/`, …), each with its own `{Module}DbContextModelSnapshot`.

```bash
dotnet ef migrations add {Name} \
  --project src/Host/FSH.Starter.Migrations.PostgreSQL \
  --startup-project src/Host/FSH.Starter.Api \
  --context {Module}DbContext
```

- **`migrations remove` operates on the snapshot** — run a full build *before* `migrations add` so the snapshot is current, or you can lose the previous migration.
- The DB is **not** migrated at API startup. The `DbMigrator` host is a separate step: `apply` (default), `seed`, `seed-demo` (dev only), `list-pending`; flags `--tenant <id>`, `--catalog-only`, `--seed`. It migrates the tenant catalog first, then each tenant's per-module schema, serialized by a Postgres advisory lock.
- `dotnet-ef` is pinned in `.config/dotnet-tools.json` — run `dotnet tool restore` first.

## Tests + EF

- Integration tests use Testcontainers (real PostgreSQL) — **Docker must be running**.
- In integration tests, set the Finbuckle tenant context **inline in the same method** as the `UserManager`/`DbContext` call; an awaited-helper set is lost (AsyncLocal) and the tenant query filter NREs.
