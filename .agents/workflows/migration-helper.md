---
description: Safely manage EF Core migrations for FSH's per-provider, per-module Migrations projects (PostgreSQL and MSSQL). Use when adding entities or changing schema. The create-migration skill holds the canonical add/apply recipe; verify-migrations checks nothing was left behind.
---

You help manage EF Core migrations safely. The canonical add/review/apply recipe is the **`create-migration`**
skill — follow it. This playbook covers the surrounding facts and troubleshooting.

## Facts (read before running commands)
- Migrations live in **one project per provider** — `src/Host/FSH.Starter.Migrations.PostgreSQL` and `src/Host/FSH.Starter.Migrations.MSSQL` — each foldered **per module/context** (`Catalog/`, `Identity/`, …) with its own `{X}DbContextModelSnapshot`. **A model change usually needs a migration in both.**
- **Run the `verify-migrations` skill after any entity/EF-config change.** `MigrationDriftTests` compares each context's live model against that provider's own snapshot and names the exact command for whatever is missing — it is what catches "generated the PostgreSQL migration, forgot the SQL Server one".
- Maintaining both providers is **not mandatory**: `FshMaintainedDbProviders` in `src/Directory.Build.props` decides which ones fail the build. A project that has settled on one engine narrows it, and the other becomes advisory.
- Not every difference between the two projects is a defect. `AddMessageChronologicalSortKey` is MSSQL-only by design, and the MSSQL migrations carry hand-written DDL (`CREATE JSON INDEX`, full-text catalog) that must survive any regeneration.
- Startup project is `src/Host/FSH.Starter.Api`. Always pass `--context {X}DbContext` and `--output-dir {X}`.
- `dotnet-ef` is pinned — `dotnet tool restore` first.
- **The DB is NOT migrated on API startup.** The `DbMigrator` host applies it: it migrates the tenant catalog (`TenantDbContext`) first, then each tenant's per-module schema, serialized by a database-held migrator lock (Postgres advisory lock, or `sp_getapplock` on SQL Server). (`UseHeroMultiTenantDatabases()` only registers Finbuckle's tenant resolution — it does not run migrations.)
- **Build before `migrations add`** — it reads the snapshot, which regenerates from a build; a stale snapshot silently loses changes. `migrations remove` rewrites the snapshot, so only ever remove the latest and rebuild after.

## Context names (real)
`IdentityDbContext`, `TenantDbContext` (the tenant catalog — **not** "MultitenancyDbContext"), `AuditDbContext`, `BillingDbContext`, `CatalogDbContext`, `TicketsDbContext`, `FilesDbContext`, `ChatDbContext`, `NotificationsDbContext`, `WebhookDbContext`.

## Apply (canonical path)
```bash
dotnet run --project src/Host/FSH.Starter.DbMigrator -- list-pending   # preview
dotnet run --project src/Host/FSH.Starter.DbMigrator -- apply [--seed]
```
(`dotnet ef database update --context {X}DbContext …` works for a single context in local dev.)

## Naming
`Add{Entity}`, `Add{Property}To{Entity}`, `Create{Index}Index`, `Rename{Old}To{New}`.

## Review the generated migration
- `dotnet ef migrations script --idempotent --context {X}DbContext …` and scan for: dropped tables/columns, non-nullable columns added to existing tables without a default, renames surfacing as drop+add (data loss).
- Check `Up()` **and** `Down()`.

## Troubleshooting
| Symptom | Cause → fix |
|---|---|
| "No DbContext was found" / multiple contexts | Always pass `--context {X}DbContext` |
| "Build failed" | `dotnet build src/FSH.Starter.slnx` first |
| Migration landed in the wrong folder | Add `--output-dir {X}` (match the context's existing folder) |
| Changes missing from the migration | You didn't build before `migrations add` (stale snapshot) |
| New module's context not found by ef | The Migrations project must reference the module's runtime project |
