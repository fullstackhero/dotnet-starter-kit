# FullStackHero .NET Starter Kit

> A production-ready modular .NET 10 monolith + two React 19 apps, built for enterprise SaaS.

This file is the canonical guide for **all** AI coding tools (Claude Code, Gemini CLI, Cursor, Codex, …).
`CLAUDE.md` and `GEMINI.md` are thin bridges that import this file — edit conventions **here**, not there.

This file is the map. Detailed conventions live in `.agents/rules/` and are read on demand — **read the
relevant rule file before working in that area** (see the index below). Keep this file lean.

## What this is

A **modular monolith** (Vertical Slice Architecture) backend that ships with two **React + Vite**
front-ends and a CLI. Multitenancy, auth, auditing, billing, files, chat and more are first-class.

- **Backend** — .NET 10, EF Core 10, PostgreSQL, Redis, JWT + ASP.NET Identity, Finbuckle multitenancy,
  Hangfire, OpenAPI/Scalar, Serilog + OpenTelemetry, .NET Aspire.
- **Frontends** — `clients/admin` (operator-facing) and `clients/dashboard` (tenant-facing): React 19,
  Vite 7, TypeScript, TanStack Query v5, React Router 7, Radix + Tailwind v4 (shadcn-style), SignalR/SSE.

## Repo map

| Path | What |
|------|------|
| `src/BuildingBlocks/` | Shared framework libraries (Core, Persistence, Web, Caching, Eventing, Storage, Quota…). **Protected — see below.** |
| `src/Modules/{Name}/` | Bounded contexts. Each has a runtime project + a `.Contracts` project (its only public API). |
| `src/Host/FSH.Starter.Api` | Composition-root Web API host. |
| `src/Host/FSH.Starter.AppHost` | .NET Aspire orchestrator (Postgres, Redis, MinIO, migrator, API, **both React apps**). |
| `src/Host/FSH.Starter.DbMigrator` | One-shot migrate/seed runner. DB is **not** migrated at API startup. |
| `src/Host/FSH.Starter.Migrations.PostgreSQL` | All EF migrations, organized per-module by folder. |
| `src/Tests/` | Per-module tests, `Architecture.Tests` (NetArchTest), `Integration.Tests` (Testcontainers). |
| `src/Tools/CLI` | The `fsh` CLI (Spectre.Console). |
| `clients/admin`, `clients/dashboard` | The two React apps. |
| `deploy/` | Infra (docker, terraform, dokploy). |

## Tech stack

| Backend | | Frontend | |
|---|---|---|---|
| Framework | .NET 10 / C# latest | Framework | React 19 + Vite 7 + TS 5.x |
| CQRS | Mediator 3.x (source-gen) | Data | TanStack Query v5 |
| Validation | FluentValidation 12.x | Routing | React Router 7 |
| ORM / DB | EF Core 10 / PostgreSQL (Npgsql) | UI | Radix + Tailwind v4 + CVA (shadcn) |
| Auth | JWT Bearer + ASP.NET Identity | Forms | react-hook-form + zod (**admin only**) |
| Multitenancy | Finbuckle 10.x | Realtime | `@microsoft/signalr`, SSE (dashboard) |
| Cache / Jobs | Redis, Hangfire | Tests | Playwright (route-mocked) |
| Docs | OpenAPI + Scalar | API client | hand-written `apiFetch` (no codegen) |
| Hosting | .NET Aspire | Env | runtime `/config.json` (not `VITE_*`) |
| Testing | xUnit, Shouldly, NSubstitute, AutoFixture, NetArchTest, Testcontainers | | |

## Build & run

```bash
# Whole stack (Postgres + pgAdmin + Redis + MinIO + migrator + API + both React apps)
dotnet run --project src/Host/FSH.Starter.AppHost   # one-time: npm install in clients/admin & clients/dashboard

dotnet build src/FSH.Starter.slnx                   # build backend
dotnet run --project src/Host/FSH.Starter.Api       # API only → https://localhost:7030 (/scalar)
dotnet test src/FSH.Starter.slnx                    # tests — integration tests REQUIRE Docker

cd clients/admin && npm install && npm run dev       # → http://localhost:5173
cd clients/dashboard && npm install && npm run dev   # → http://localhost:5174
```

Migrations / seed (DbMigrator, separate step):
```bash
dotnet run --project src/Host/FSH.Starter.DbMigrator -- apply [--seed]
dotnet run --project src/Host/FSH.Starter.DbMigrator -- list-pending
```

**Ports:** API 7030 (https)/5030 (http) · admin 5173 · dashboard 5174 · Postgres 5432 · pgAdmin 5050 · Valkey 6379 · MinIO 9000/9001.

## Branching & PRs

Single long-lived branch: **`main`** (the default) — there is **no `develop`**. Branch from and target `main`; stable releases are cut from `v*` tags. CI is split into path-scoped **Backend CI** (`src/**`) and **Frontend CI** (`clients/**`) workflows; branch protection requires only those two gate checks — never the individual jobs, which are skipped on the other side's PRs.

## Golden rules (do not break)

1. **Module boundaries** — a module references another module only through its `.Contracts` project, never its runtime project. Enforced by `Architecture.Tests`.
2. **Registering a module touches FOUR places** — `Program.cs` Mediator `o.Assemblies` (two markers each) + `moduleAssemblies` array, **and the identical pair in `DbMigrator/Program.cs`**. A missing Mediator marker = handlers silently undiscovered. See `architecture.md`.
3. **Tenant isolation is default-ON** via `BaseDbContext`. Opt out only via `IGlobalEntity`. Subclass DbContexts call `base.OnModelCreating` **last**. See `database.md`.
4. **Do NOT modify `src/BuildingBlocks`** without explicit approval — shared by every module, wide blast radius.
5. **Mediator handlers must be `public sealed`**, return `ValueTask<T>`, and `.ConfigureAwait(false)` every await.
6. **Structured logging only** — no string interpolation in log messages; use message templates / `[LoggerMessage]`.
7. **Propagate `CancellationToken`** into every EF/IO call; add as `= default` on public service methods.
8. **Every command handler + paginated query handler needs a validator** (`{Name}Validator`). Enforced by `Architecture.Tests`.
9. **Frontend: pass per-call data through `mutate(arg)`**, never via state the mutation callbacks close over (execute-time race). See `frontend/shared.md`.
10. **Docs + changelog travel with the change** — a user-facing change (feature, endpoint, config, infra, breaking change) isn't done until the **separate docs repo** (`github.com/fullstackhero/docs`, the Astro site) is updated to match **and** a changelog entry is added (`src/content/docs/changelog/`). Don't let the docs drift from the code.

## Rules index — read the relevant file before you work

**Backend / cross-cutting** (`.agents/rules/`)

| Working on… | Read |
|---|---|
| Module structure, boundaries, registration, DI, middleware order, config | `architecture.md` |
| Endpoints, CQRS, validation, exceptions, permissions, versioning | `api-conventions.md` |
| EF Core, entities, migrations, tenant isolation, query filters | `database.md` |
| Cross-module events, Outbox/Inbox, idempotent handlers | `eventing.md` |
| Caching (HybridCache/Redis), keys, invalidation | `caching.md` |
| Background jobs (Hangfire), recurring jobs | `jobs.md` |
| Outbound HTTP resilience (Polly) | `resilience.md` |
| Files/blobs, presigned uploads, providers | `storage.md` |
| CORS, security headers, rate limiting, idempotency, quotas | `security.md` |
| SignalR / SSE backend | `realtime.md` |
| Logging, correlation, OpenTelemetry | `logging.md` |
| Unit test conventions, NetArchTest | `testing.md` |
| Integration tests (Testcontainers harness + gotchas) | `integration-testing.md` |
| **Modifying `src/BuildingBlocks`** (read first — it's protected) | `buildingblocks-protection.md` |
| A specific module's quirks | `modules/{module}.md` (identity, multitenancy, chat, files, webhooks, auditing, billing, catalog, tickets, notifications) |

**Frontend** (`.agents/rules/frontend/`)

| Working on… | Read |
|---|---|
| Any React work (shared stack, API client, Query, Tailwind, design language) | `frontend/shared.md` |
| The operator app (`clients/admin`) | `frontend/admin.md` |
| The tenant app (`clients/dashboard`) | `frontend/dashboard.md` |

## Coding style (backend)

File-scoped namespaces · 4-space indent · explicit types (`var` only when RHS-obvious) · `is null` /
`is not null` · pattern matching + switch expressions · `ArgumentNullException.ThrowIfNull` guards ·
records for DTOs/events/value objects · `default!` for required non-nullable strings. Build runs with
`TreatWarningsAsErrors` — warnings fail the build.

## Adding things (quick pointers)

- **Feature** — Contracts command/query → handler → validator → endpoint → wire in module `MapEndpoints()` → tests. Details: `api-conventions.md`.
- **Module** — new `Modules.{Name}` + `.Contracts`, implement `IModule` w/ assembly-level `[assembly: FshModule(typeof(XModule), order)]`, register in **all four places**, add migration folder + tests. Details: `architecture.md`.
- **React page** — API module (`src/api/`) → page → register lazy route → (admin) mirror permission + RouteGuard → Playwright test. Details: `frontend/shared.md`.

## AI tooling resources

- **Rules** — `.agents/rules/*.md` (indexed above). Read on demand.
- **Skills** — `.agents/skills/*/SKILL.md`: step-by-step task recipes. Scaffolders: `add-feature`, `add-entity`, `add-module`, `add-react-page`, `add-full-slice`. Ops: `create-migration`, `add-integration-event`, `add-permission`. Reference: `query-patterns`, `testing-guide`, `mediator-reference`.
- **Workflows** — `.agents/workflows/*.md`: task playbooks (`code-reviewer`, `feature-scaffolder`, `module-creator`, `architecture-guard`, `migration-helper`).
