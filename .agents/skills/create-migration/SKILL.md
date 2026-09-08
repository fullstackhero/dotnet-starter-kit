---
name: create-migration
description: Create and apply an EF Core migration for a module's DbContext the FSH way (one Migrations project per provider, per-module folder, correct --context). Use after changing entities/EF config — every change needs a migration for BOTH PostgreSQL and SQL Server. See .agents/rules/database.md.
argument-hint: "[ModuleName] [MigrationName]"
---

# Create Migration

Migrations live in **one project per provider** — `src/Host/FSH.Starter.Migrations.PostgreSQL` and
`src/Host/FSH.Starter.Migrations.MSSQL` — each foldered **per module/context** (`Catalog/`, `Identity/`, …)
with its own `{X}DbContextModelSnapshot`. The DB is **not** migrated at API startup; the `DbMigrator`
host applies it.

> **Both providers, every time.** An entity or EF-config change that produces a PostgreSQL migration
> must produce the matching SQL Server one, or the MSSQL deployment silently drifts. Step 2 runs twice.

## Step 0 — restore the pinned tool (first time)

```bash
dotnet tool restore     # dotnet-ef is pinned in .config/dotnet-tools.json
```

## Step 1 — BUILD FIRST (snapshot footgun)

`dotnet ef migrations add` reads the **current snapshot**, which is regenerated from a build. If you skip
the build after editing entities/config, you can generate against a stale snapshot and lose changes. Also,
`migrations remove` rewrites the snapshot — only remove the latest, and rebuild after.

```bash
dotnet build src/FSH.Starter.slnx
```

## Step 2 — add the migration

Specify **all three** of `--project` (the Migrations project), `--startup-project` (the API host), and
`--context {X}DbContext`. Use `--output-dir {X}` so it lands in that module's folder (match the existing
folder for the context).

```bash
# 2a — PostgreSQL
dotnet ef migrations add {MigrationName} \
  --project src/Host/FSH.Starter.Migrations.PostgreSQL \
  --startup-project src/Host/FSH.Starter.Api \
  --context {X}DbContext \
  --output-dir {X}
```

```bash
# 2b — SQL Server. The env vars decide which provider the design-time model is built for; without
# them you would scaffold PostgreSQL DDL into the MSSQL project. Needs a reachable SQL Server 2025.
DatabaseOptions__Provider=MSSQL \
DatabaseOptions__MigrationsAssembly=FSH.Starter.Migrations.MSSQL \
DatabaseOptions__ConnectionString='Server=localhost,1433;Database=fsh;User Id=sa;Password=…;TrustServerCertificate=True' \
dotnet ef migrations add {MigrationName} \
  --project src/Host/FSH.Starter.Migrations.MSSQL \
  --startup-project src/Host/FSH.Starter.Api \
  --context {X}DbContext \
  --output-dir {X}
```

Then confirm neither provider has drifted. The fastest check is the **`verify-migrations`** skill
(`MigrationDriftTests`, no database, under a second), which names the exact command for anything
missing. The CLI equivalent, for one-off diagnosis:

```bash
dotnet ef migrations has-pending-model-changes --context {X}DbContext \
  --project src/Host/FSH.Starter.Migrations.PostgreSQL --startup-project src/Host/FSH.Starter.Api
# …and the same with the MSSQL env vars + --project …Migrations.MSSQL
```

## Step 3 — review the generated SQL before applying

```bash
dotnet ef migrations script --idempotent \
  --project src/Host/FSH.Starter.Migrations.PostgreSQL \
  --startup-project src/Host/FSH.Starter.Api \
  --context {X}DbContext
```

Check for: unintended table/column drops, non-nullable columns added without a default to an existing
table, and renames surfacing as drop+add (data loss). Adjust the model or hand-edit the migration if needed.

## Step 4 — apply

Preferred (the canonical path — migrates the tenant catalog then each tenant's per-module schema):

```bash
dotnet run --project src/Host/FSH.Starter.DbMigrator -- apply
dotnet run --project src/Host/FSH.Starter.DbMigrator -- list-pending   # to preview first
```

(Or, single-context local dev, `dotnet ef database update --context {X}DbContext --project … --startup-project …`.)

## Notes

- A **new module** also needs a `{X}/` folder in **both** Migrations projects, and the runtime project referenced from both — see `add-module`.
- `dotnet ef` against a `BaseDbContext` works because the 4-arg ctor is satisfied by the startup host's DI.
- Write EF config with the **portable helpers** (`.HasJsonColumn()`, `.HasNotDeletedFilter()`, …), never provider SQL literals — see the provider table in `.agents/rules/database.md`. A literal fails model-build on the other provider.
- Some DDL has no EF API and is hand-written into the MSSQL migration: the audit `CREATE JSON INDEX` and the chat full-text catalog. Preserve those blocks if you ever regenerate those migrations.

## Checklist

- [ ] `dotnet tool restore` done (first time)
- [ ] Built **before** `migrations add`
- [ ] `--context {X}DbContext` + `--output-dir {X}` (lands in the right folder)
- [ ] Migration added for **both** providers (PostgreSQL *and* MSSQL)
- [ ] `verify-migrations` skill green for every maintained provider (advisory drift on the others reviewed)
- [ ] Reviewed the generated SQL for data loss
- [ ] Applied via DbMigrator `apply` (or `ef database update` for one context locally)
