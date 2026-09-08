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

## Database providers (PostgreSQL + SQL Server)

- The provider is **config-selected**, never hardcoded: `DatabaseOptions:Provider` (`POSTGRESQL` | `MSSQL`) plus a matching `DatabaseOptions:MigrationsAssembly`. Both migrations projects ship in every build, so switching is a config change — no rescaffold, no file surgery.
- **MSSQL requires SQL Server 2025 (17.x) or Azure SQL.** JSON columns map to the native `json` type (compatibility level 170); it does not exist on 2019/2022 and those migrations will not apply there.
- **Never write provider SQL in an entity configuration.** A literal `HasColumnType("jsonb")` or `HasFilter("\"IsDeleted\" = FALSE")` locks the model to one provider and fails model-build on the other. Declare intent instead — `HeroProviderConventions` resolves it per provider:

| Instead of | Write | PostgreSQL | SQL Server |
|---|---|---|---|
| `HasColumnType("jsonb")` | `.HasJsonColumn()` | `jsonb` | `json` |
| `HasColumnType("text")` | `.HasUnboundedTextColumn()` | `text` | `nvarchar(max)` |
| `HasDefaultValueSql("'{}'::jsonb")` | `.HasJsonDefaultEmptyObject()` | `'{}'::jsonb` | `N'{}'` |
| `HasDefaultValueSql("CURRENT_TIMESTAMP")` | `.HasUtcNowDefault()` | `CURRENT_TIMESTAMP` | `SYSUTCDATETIME()` |
| `HasFilter("\"IsDeleted\" = FALSE")` | `.HasNotDeletedFilter()` | `"IsDeleted" = FALSE` | `[IsDeleted] = 0` |
| `HasFilter("\"Status\" = 1")` | `.HasEqualsFilter("Status", 1)` | `"Status" = 1` | `[Status] = 1` |
| `.HasMethod("gin").HasOperators("gin_trgm_ops")` | `.AsTrigramSearchIndex()` | trigram GIN | *removed from model* |
| `.HasMethod("gin").HasOperators("jsonb_path_ops")` | `.AsJsonContainmentIndex()` | `jsonb_path_ops` GIN | *removed; `CREATE JSON INDEX` in the migration* |

- A DbContext that does **not** derive from `BaseDbContext` must register the conventions itself in a `ConfigureConventions` override (`IdentityDbContext`, `TenantDbContext`, `BillingDbContext` do). `ProviderConventionRegistrationTests` fails the build if a new one forgets — the symptom otherwise is silent: JSON columns become `text`, every partial-index filter vanishes, and `Fsh:*` annotations leak into the snapshot and break the scaffolder.
- **Queries:** use `.WhereSearch(db.Database, term, x => x.Name, …)` rather than `EF.Functions.ILike` — `ILike` is Npgsql-only. For matching a JSON property as text, build the pattern with `ProviderQueryExtensions.JsonTextPropertyPattern`: PostgreSQL renders `jsonb::text` as `{"k": "v"}` (space after the colon), SQL Server's `json` casts to `{"k":"v"}` (no space), so a hard-coded pattern silently matches nothing on the other provider.
- **Capability differences on SQL Server:**
  - Audit `Source`/`UserName` substring search scans — no trigram equivalent for `%term%` on `nvarchar(max)`.
  - Chat message search uses `FREETEXTTABLE` when the instance has Full-Text Search, and falls back to a `LIKE` scan when it does not (the official `mcr.microsoft.com/mssql/server` container has no FTS; Azure SQL and full installs do). SQL Server populates a full-text index **asynchronously**, unlike PostgreSQL's generated `tsvector` column, so the FTS path is UNIONed with a `LIKE` pass over the last few minutes — otherwise a just-sent message would be silently unsearchable. Those pending matches are returned first, newest-first, ahead of the ranked ones.
  - Guid v7 ids do **not** sort chronologically on SQL Server: `uniqueidentifier` compares the last six bytes first. Message paging therefore sorts on a persisted `char(36)` sort key (`ChatDbContext.MessageSortKey`) rather than on `Id`. Any new cursor pagination keyed on a Guid needs the same treatment — see `MessageOrdering`.
- **Raw SQL + `Include` on SQL Server:** EF wraps a `FromSql` query in a subselect to resolve the includes, and `SELECT … FROM (WITH … SELECT …) t` is not valid T-SQL. Use derived tables, never a CTE, in a `FromSql` that gets composed with `Include`.
- `EnableRetryOnFailure` is deliberately **off** for MSSQL: its execution strategy refuses user-initiated transactions, which the outbox requires.

## Migrations

Migrations live in **one project per provider** — `src/Host/FSH.Starter.Migrations.PostgreSQL` and `src/Host/FSH.Starter.Migrations.MSSQL` — each organized **per-module by folder** (`Identity/`, `Catalog/`, `Chat/`, …) with its own `{Module}DbContextModelSnapshot`. **An entity change needs a migration in BOTH.**

```bash
# PostgreSQL (default provider)
dotnet ef migrations add {Name} \
  --project src/Host/FSH.Starter.Migrations.PostgreSQL \
  --startup-project src/Host/FSH.Starter.Api \
  --context {Module}DbContext

# SQL Server — the env vars pick the provider the design-time model is built against
DatabaseOptions__Provider=MSSQL \
DatabaseOptions__MigrationsAssembly=FSH.Starter.Migrations.MSSQL \
DatabaseOptions__ConnectionString='Server=localhost,1433;Database=fsh;User Id=sa;Password=…;TrustServerCertificate=True' \
dotnet ef migrations add {Name} \
  --project src/Host/FSH.Starter.Migrations.MSSQL \
  --startup-project src/Host/FSH.Starter.Api \
  --context {Module}DbContext
```

- **Verify with the `verify-migrations` skill** (`MigrationDriftTests` in `Architecture.Tests`): it compares every context's live model against that provider's own snapshot — the in-process equivalent of `has-pending-model-changes` — with no database and no Docker, and prints the exact `migrations add` command for whatever is missing. It is what catches "generated the PostgreSQL migration, forgot the SQL Server one", and also a portable-intent helper resolving differently than expected. The CLI command remains useful for one-off diagnosis.
- **Maintaining both providers is not mandatory.** `FshMaintainedDbProviders` in `src/Directory.Build.props` (`both` | `POSTGRESQL` | `MSSQL`, override per run with `FSH_MIGRATIONS_PROVIDERS`) decides which providers *fail* the build on drift; the rest only report it. Ships as `both` because the bundled modules are kept in sync for both engines — narrow it once a project settles on one, so nobody is blocked by migrations for an engine they do not use.

- **`migrations remove` operates on the snapshot** — run a full build *before* `migrations add` so the snapshot is current, or you can lose the previous migration.
- The DB is **not** migrated at API startup. The `DbMigrator` host is a separate step: `apply` (default), `seed`, `seed-demo` (dev only), `list-pending`; flags `--tenant <id>`, `--catalog-only`, `--seed`. It migrates the tenant catalog first, then each tenant's per-module schema, serialized by a database-held migrator lock (a Postgres advisory lock, or `sp_getapplock` on SQL Server).
- `dotnet-ef` is pinned in `.config/dotnet-tools.json` — run `dotnet tool restore` first.

## Tests + EF

- Integration tests use Testcontainers (real PostgreSQL by default) — **Docker must be running**. Set `FSH_TEST_DB_PROVIDER=MSSQL` to run the same suite against SQL Server 2025.
- In integration tests, set the Finbuckle tenant context **inline in the same method** as the `UserManager`/`DbContext` call; an awaited-helper set is lost (AsyncLocal) and the tenant query filter NREs.
