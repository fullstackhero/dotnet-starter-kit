---
name: create-migration
description: Create and apply an EF Core migration for a module's DbContext the FSH way (central Migrations project, per-module folder, correct --context). Use after changing entities/EF config. See .agents/rules/database.md.
argument-hint: "[ModuleName] [MigrationName]"
---

# Create Migration

All migrations live in **one** project — `src/Host/FSH.Starter.Migrations.PostgreSQL` — but are foldered
**per module/context** (`Catalog/`, `Identity/`, …), each with its own `{X}DbContextModelSnapshot`. The DB
is **not** migrated at API startup; the `DbMigrator` host applies it.

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
dotnet ef migrations add {MigrationName} \
  --project src/Host/FSH.Starter.Migrations.PostgreSQL \
  --startup-project src/Host/FSH.Starter.Api \
  --context {X}DbContext \
  --output-dir {X}
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

- A **new module** also needs a `{X}/` folder in the Migrations project and the runtime project referenced from it — see `add-module`.
- `dotnet ef` against a `BaseDbContext` works because the 4-arg ctor is satisfied by the startup host's DI.

## Checklist

- [ ] `dotnet tool restore` done (first time)
- [ ] Built **before** `migrations add`
- [ ] `--context {X}DbContext` + `--output-dir {X}` (lands in the right folder)
- [ ] Reviewed the generated SQL for data loss
- [ ] Applied via DbMigrator `apply` (or `ef database update` for one context locally)
