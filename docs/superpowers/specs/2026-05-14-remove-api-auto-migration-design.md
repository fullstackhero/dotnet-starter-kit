# Remove auto-migration from the API — production-ready DbMigrator-only path

**Status:** Approved (2026-05-14)
**Author:** Mukesh Murugan
**Scope:** Single sprint, one PR.

## Problem

The API still owns two startup-time migration paths that duplicate work the
standalone `FSH.Starter.DbMigrator` project already does correctly:

1. `TenantStoreInitializerHostedService` unconditionally calls
   `TenantDbContext.Database.MigrateAsync()` and seeds the root tenant on
   every API boot.
2. `TenantAutoProvisioningHostedService` enqueues per-tenant
   migrate/seed jobs whenever
   `MultitenancyOptions.RunTenantMigrationsOnStartup=true` or
   `AutoProvisionOnStartup=true`.

The well-known production problems with API-side auto-migration apply
here: the API needs DDL privileges on every database, replicas race on
startup, deployment and runtime concerns are mixed, and a partially-
migrated state is silently served to traffic.

The `FSH.Starter.DbMigrator` console project (Postgres advisory lock,
exponential DB-ready backoff, exit codes, per-tenant pass via the same
`ITenantService.MigrateTenantAsync` the runtime uses) already covers
deploy-time migrations. Aspire's `AppHost.cs` already chains it as a
prerequisite to the API via `.WaitForCompletion(migrator)`.

## Goal

The API has **zero** schema-changing code paths at startup. The
DbMigrator is the only path that runs migrations. The on-demand tenant
provisioning flow (`POST /tenants` → `TenantProvisioningJob`) is
preserved — that's runtime provisioning of a new tenant's schema, not
deploy-time migration of existing tenants.

## Non-goals

- The DbMigrator project itself is **not** being rewritten — it is already
  production-grade. Only its README and two small fail-fast checks are touched.
- Module-level `IDbInitializer` implementations stay as-is. They're still
  called by `TenantService.MigrateTenantAsync`, which is still invoked by
  both DbMigrator (deploy-time) and `TenantProvisioningJob` (runtime new-tenant).
- The integration-test factory (`FshWebApplicationFactory`) continues to
  migrate in-process — it's a test concern, not a deployment concern.

## Design

### 1. API code removals

**Delete entire files:**

- `src/Modules/Multitenancy/Modules.Multitenancy/Provisioning/TenantStoreInitializerHostedService.cs`
- `src/Modules/Multitenancy/Modules.Multitenancy/Provisioning/TenantAutoProvisioningHostedService.cs`

**Edit `src/Modules/Multitenancy/Modules.Multitenancy/MultitenancyModule.cs`:**

Remove these two service registrations (currently lines 50 and 52):

```csharp
builder.Services.AddHostedService<TenantStoreInitializerHostedService>();
// ...
builder.Services.AddHostedService<TenantAutoProvisioningHostedService>();
```

### 2. Config-flag removals

**Edit `src/Modules/Multitenancy/Modules.Multitenancy/MultitenancyOptions.cs`:**

Delete both properties:

- `RunTenantMigrationsOnStartup`
- `AutoProvisionOnStartup`

The class becomes empty (or is removed entirely if no other options
exist on it). Decision: delete the file too, plus its `AddOptions` binding
in `MultitenancyModule.cs`. There is no business meaning left for the
options class once both flags are gone.

**Scrub from all appsettings files:**

- `src/Host/FSH.Starter.Api/appsettings.json` — remove the entire
  `MultitenancyOptions` block (lines 155–158).
- `src/Host/FSH.Starter.Api/appsettings.Development.json` — remove
  `MultitenancyOptions` block (lines 14–17).
- `src/Host/FSH.Starter.Api/appsettings.Production.json` — remove
  `MultitenancyOptions` block (lines 94–96).

**Scrub from test fixtures:**

- `src/Tests/Integration.Tests/Infrastructure/FshWebApplicationFactory.cs`
  line 129: remove
  `["MultitenancyOptions:RunTenantMigrationsOnStartup"] = "false"`.
- `src/Tests/Multitenancy.Tests/MultitenancyOptionsTests.cs`: delete
  the entire test file. Every test in it is about the two removed flags.

**Scrub from docs:**

- `docs/src/content/docs/multitenancy.mdx` — remove the
  `RunTenantMigrationsOnStartup` entry (line 144 + the table row at
  line 152).
- `docs/src/content/docs/tenant-provisioning.mdx` — remove the
  `RunTenantMigrationsOnStartup` reference (line 62).
- `src/Host/FSH.Starter.DbMigrator/README.md` — remove the
  "Local development" paragraph (lines 114–122) that documents the
  API self-migrating in dev.

### 3. Health-check upgrade (the production guarantee)

**Edit `src/Modules/Multitenancy/Modules.Multitenancy/TenantMigrationsHealthCheck.cs`:**

Current behavior: always returns `HealthCheckResult.Healthy("Tenant
migrations status collected.", details)` — even when pending
migrations exist. It collects data but does not gate.

New behavior:

- If **any** tenant's `TenantDbContext` has pending migrations, return
  `HealthCheckResult.Unhealthy(...)` with the offending tenants + names
  in `details`.
- If a per-tenant check throws, return `Unhealthy` (current code
  swallows the exception into `details` as `Error = ex.Message`; that
  hides drift).
- Otherwise return `Healthy`.

This is the production-readiness teeth: `/health/ready` already
returns 503 when any check is `Unhealthy`
(`HealthEndpoints.cs:57–59`), and k8s/load-balancer readiness probes
pull the pod out of rotation. So if DbMigrator hasn't run, no traffic
hits the API against a stale schema — and the operator sees exactly
which tenants are pending in the `/health/ready` JSON.

### 4. DbMigrator fail-fast hardening

Two minimal hardening changes to `src/Host/FSH.Starter.DbMigrator/Program.cs`:

1. **Empty-connection-string check** (before any retry loop, currently
   line 152): explicit "DatabaseOptions:ConnectionString is empty —
   refusing to run against an unconfigured target. Set
   `DatabaseOptions__ConnectionString` to an elevated-DDL connection
   string." with exit code 1.
2. **Log the connected role.** Run `SELECT current_user, current_database()`
   on first successful connection and log it at Information level.
   Lets operators catch a misconfigured low-priv connection string
   immediately rather than at the first `permission denied` during
   `MigrateAsync`.

### 5. CI gate for the migrator container

Add a GitHub Actions job (or update the existing CI workflow) that:

1. Publishes the migrator container: `dotnet publish src/Host/FSH.Starter.DbMigrator -c Release /t:PublishContainer /p:ContainerRepository=fsh-db-migrator`.
2. Spins up an ephemeral Postgres service container.
3. Runs the published image with `apply --catalog-only` against it.
4. Asserts exit code 0.

Catches container-publish regressions and any DI-graph breakage in the
migrator before deploy.

### 6. Integration test — locking in the new contract

Add `src/Tests/Integration.Tests/Tests/Health/MigrationReadinessTests.cs`:

- Boots the API with an empty Postgres (no migrations applied).
- Asserts `GET /health/ready` returns 503 and the body contains
  `db:tenants-migrations` as `Unhealthy`.
- Runs the per-tenant migrate path in-process (existing test-factory
  helper).
- Asserts `GET /health/ready` returns 200.

This is the regression test that prevents anyone from re-introducing
the silent auto-migration.

## What stays unchanged

- `TenantProvisioningJob.RunAsync` — still called when `POST /tenants`
  creates a new tenant. The job's existing
  `TenantService.MigrateTenantAsync` + `SeedTenantAsync` calls remain.
  This is on-demand tenant onboarding, not startup migration.
- `TenantService.MigrateTenantAsync` and `SeedTenantAsync` — shared by
  DbMigrator and `TenantProvisioningJob`. Untouched.
- All per-module `IDbInitializer` implementations. Untouched.
- Aspire `AppHost.cs` — already correct
  (`api.WaitForCompletion(migrator)`).
- All other module health checks (`AddDbContextCheck<*>` per module).
  These already correctly fail when their underlying DB is
  unreachable; combined with the upgraded
  `TenantMigrationsHealthCheck` they form a complete readiness story.

## Operational model after this change

| Environment | How migrations run |
|---|---|
| Local Aspire (`dotnet run --project AppHost`) | Aspire runs DbMigrator first, API waits for completion, API boots against a current schema. Unchanged. |
| Local raw (`dotnet run --project Api`) | Developer runs `dotnet run --project src/Host/FSH.Starter.DbMigrator -- apply --seed` once after pulling. API boots green. If they forget, `/health/ready` returns 503 — clear signal, not cryptic EF errors. |
| Containers (Helm / Compose) | `pre-install` / `pre-upgrade` job runs the migrator image with elevated DDL credentials; API deploys with a runtime-only (no DDL) connection string. Already documented in DbMigrator README. |
| CI integration tests | `FshWebApplicationFactory` migrates in-process per test run. Unchanged. |
| New tenant via `POST /tenants` | `TenantProvisioningJob` migrates+seeds that tenant's schema on demand. Unchanged. |

## Risk and rollout

- **Single-PR change.** The flags being removed default to `false` in
  production already, so prod behavior does not change. The two hosted
  services are dead code in any environment that already runs the
  DbMigrator.
- **The breaking surface is config:** removing the two
  `MultitenancyOptions` flags. Anyone who set them in their own
  `appsettings.Production.json` will need to remove those keys (binding
  is permissive, so unknown keys won't fail, but the docs should call
  it out in CHANGELOG).
- **Downstream forks** that depended on the API self-migrating need to
  add a DbMigrator step to their pipeline. CHANGELOG entry + DbMigrator
  README updates cover this.
- **No rollback complexity.** Reverting is a single PR revert; the
  DbMigrator does not record any state the API depends on.

## Acceptance criteria

1. The two hosted-service files are deleted; nothing in the API still
   calls `Database.MigrateAsync()` at startup time. (`grep MigrateAsync src/Host/FSH.Starter.Api src/Modules` shows only the on-demand path + IDbInitializer implementations.)
2. `MultitenancyOptions` and its config keys do not exist anywhere in
   the repo. (`grep RunTenantMigrationsOnStartup src` returns nothing
   outside this design doc.)
3. `TenantMigrationsHealthCheck` returns `Unhealthy` when pending
   migrations exist. New unit/integration test covers both cases.
4. `MigrationReadinessTests` passes against an empty database (503
   before migrations, 200 after).
5. DbMigrator CI smoke job passes.
6. `dotnet test src/FSH.Starter.slnx` is green.

## File-by-file edit list

```
DELETED
  src/Modules/Multitenancy/Modules.Multitenancy/Provisioning/TenantStoreInitializerHostedService.cs
  src/Modules/Multitenancy/Modules.Multitenancy/Provisioning/TenantAutoProvisioningHostedService.cs
  src/Modules/Multitenancy/Modules.Multitenancy/MultitenancyOptions.cs
  src/Tests/Multitenancy.Tests/MultitenancyOptionsTests.cs

EDITED
  src/Modules/Multitenancy/Modules.Multitenancy/MultitenancyModule.cs   — drop 2 AddHostedService + AddOptions<MultitenancyOptions>
  src/Modules/Multitenancy/Modules.Multitenancy/TenantMigrationsHealthCheck.cs   — Unhealthy on drift / per-tenant error
  src/Host/FSH.Starter.DbMigrator/Program.cs                            — empty-CS guard + current_user log
  src/Host/FSH.Starter.DbMigrator/README.md                             — drop local-dev section, document /health/ready contract
  src/Host/FSH.Starter.Api/appsettings.json                             — remove MultitenancyOptions block
  src/Host/FSH.Starter.Api/appsettings.Development.json                 — remove MultitenancyOptions block
  src/Host/FSH.Starter.Api/appsettings.Production.json                  — remove MultitenancyOptions block
  src/Tests/Integration.Tests/Infrastructure/FshWebApplicationFactory.cs — drop dead exclusion + the removed option override
  docs/src/content/docs/multitenancy.mdx                                — strip the two options
  docs/src/content/docs/tenant-provisioning.mdx                         — strip RunTenantMigrationsOnStartup reference
  .github/workflows/ci.yml                                              — add migrator-container smoke job

ADDED
  src/Tests/Integration.Tests/Tests/Health/MigrationReadinessTests.cs   — 503-then-200 contract test
```
