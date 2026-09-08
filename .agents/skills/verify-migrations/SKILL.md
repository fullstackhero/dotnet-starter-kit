---
name: verify-migrations
description: Check that every DbContext's migrations are up to date with the model, for each database provider this project maintains. Run before committing an entity or EF-config change. Catches the "generated the PostgreSQL migration, forgot the SQL Server one" mistake. See .agents/rules/database.md.
---

# Verify Migrations

With one migrations project per provider (`FSH.Starter.Migrations.PostgreSQL`,
`FSH.Starter.Migrations.MSSQL`), forgetting the second one **fails nothing**: the build passes, the
tests pass, the API starts. The forgotten provider simply deploys against a stale schema.

`MigrationDriftTests` (in `Architecture.Tests`) compares each context's live model against that
provider's own snapshot — the in-process equivalent of `has-pending-model-changes`. No database, no
Docker, under a second.

## Step 1 — build

The guard reads the compiled model, so a stale build gives a stale answer.

```bash
dotnet build src/FSH.Starter.slnx
```

## Step 2 — run the guard

```bash
dotnet test src/Tests/Architecture.Tests --no-build \
  --filter "FullyQualifiedName~MigrationDriftTests" \
  --logger "console;verbosity=detailed"
```

`verbosity=detailed` matters: advisory findings for an unmaintained provider are printed rather than
asserted, and the default logger hides output from passing tests.

## Step 3 — read the result

- **Green, no output** — nothing to do.
- **Failure** — a provider this project maintains is behind. The message names the context, the
  provider, and the exact `dotnet ef migrations add` command. Run it (fill in `<Name>` and
  `<Folder>`), rebuild, re-run.
- **`[migration-drift]` line but green** — a provider this project does *not* maintain is behind.
  Not fatal; fix it only if you still intend to deploy that engine.

To check both providers regardless of what the project maintains:

```bash
FSH_MIGRATIONS_PROVIDERS=both dotnet test src/Tests/Architecture.Tests --no-build \
  --filter "FullyQualifiedName~MigrationDriftTests"
```

## Which providers are enforced

`FshMaintainedDbProviders` in `src/Directory.Build.props` (`both` | `POSTGRESQL` | `MSSQL`), with the
`FSH_MIGRATIONS_PROVIDERS` env var as a per-run override. Ships as `both` because the modules bundled
with the kit are kept in sync for both engines. **Once a project settles on one engine, narrow it** —
then the other becomes advisory and never blocks a build over migrations for an engine nobody uses.

## Notes

- **Not every difference between the two projects is a mistake.** `AddMessageChronologicalSortKey`
  exists only in MSSQL, because the Guid v7 sort key is added only when
  `ChatDbContext` sees SQL Server. Likewise the trigram and JSON-containment indexes are removed from
  the MSSQL model on purpose. The guard already accounts for this: it compares each provider against
  its own snapshot, never one snapshot against the other.
- **Hand-written DDL must survive a regeneration.** The MSSQL migrations carry SQL that EF has no API
  for: `CREATE JSON INDEX` on `audit.AuditRecords`, and the chat full-text catalog. If you ever
  regenerate those migrations, re-apply those blocks.
- A missing snapshot for a context in one project (a new module that only got a folder in one
  migrations project) is reported by `Every_Context_Should_Have_A_Snapshot_In_Both_Providers`.

## Checklist

- [ ] Built before running the guard
- [ ] Guard green for every maintained provider
- [ ] Advisory drift reviewed, and either fixed or consciously accepted
- [ ] Generated migrations reviewed for data loss before applying (see `create-migration`)
