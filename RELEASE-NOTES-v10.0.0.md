# FullStackHero .NET Starter Kit — v10.0.0 (GA)

**Release date:** 2026-06-20
**Status:** General Availability
**Packages:** [`FullStackHero.CLI`](https://www.nuget.org/packages/FullStackHero.CLI) · [`FullStackHero.NET.StarterKit`](https://www.nuget.org/packages/FullStackHero.NET.StarterKit)
**Docs:** [fullstackhero.net](https://fullstackhero.net) · **Source:** [github.com/fullstackhero/dotnet-starter-kit](https://github.com/fullstackhero/dotnet-starter-kit)

---

## TL;DR

v10 is the first **General Availability** release of the FullStackHero .NET Starter Kit on **.NET 10**, and the first to ship as **public NuGet packages**: a global `fsh` CLI and a `dotnet new` template. It is a production-ready **modular monolith** (Vertical Slice Architecture, CQRS) backend with **two React 19 front-ends**, ten first-class business modules, multitenancy, real-time, billing, and one-command local orchestration via **.NET Aspire** — all delivered as **fully-owned, detached source** with **no runtime framework NuGet dependencies**.

Scaffold in two commands:

```bash
dotnet tool install -g FullStackHero.CLI
fsh new MyApp
```

---

## Headlines

- **Now on .NET 10** end-to-end — runtime, EF Core 10, Identity, SignalR, JWT bearer, and the Aspire 13.4 AppHost.
- **NuGet distribution** — `fsh` CLI (`new` / `doctor` / `info` / `update`) plus a `dotnet new fsh` template. GitHub "Use this template" remains as a discovery funnel.
- **Source-ownership model, enforced in the build** — the template scaffolds the complete source tree. There are **no per-module runtime packages**; `dotnet pack` of the solution emits only the CLI (and the template pack). You own and can change every BuildingBlock, Module, and Host project.
- **Two polished React 19 apps** — `clients/admin` (operator console) and `clients/dashboard` (tenant app), both Vite 7 + Tailwind v4, with real-time SignalR/SSE.
- **Ten business modules** wired and tested out of the box: Identity, Multitenancy, Billing, Catalog, Tickets, Chat, Files, Webhooks, Auditing, Notifications.
- **One-command local stack** — `dotnet run` the Aspire AppHost brings up Postgres, Valkey, MinIO, pgAdmin, RedisInsight, the migrator, demo seeder, the API, and both React apps.
- **Deep test coverage** — ~1,790 backend tests (unit + NetArchTest architecture + Testcontainers integration) and 214 Playwright E2E tests across the two apps. The build runs **warnings-as-errors**.

---

## Installation & getting started

### Option 1 — the `fsh` CLI (recommended)

```bash
dotnet tool install -g FullStackHero.CLI
fsh doctor          # verify SDK, Docker, Aspire, and free ports
fsh new MyApp       # interactive wizard (or pass flags below)
cd MyApp
dotnet run --project src/Host/MyApp.AppHost
```

CLI flags for `fsh new`:

| Flag | Effect |
|------|--------|
| *(none)* | Full stack: backend + Aspire AppHost + both React apps |
| `--no-frontend` | Backend + Aspire only (omit `clients/`) |
| `--no-aspire` | Omit the Aspire AppHost (run the API/migrator directly) |
| `--no-aspire --no-frontend` | Minimal API + DbMigrator only |
| `--skip-install` | Skip `npm install` for the React apps |
| `--non-interactive` | Use defaults, no prompts |
| `-o, --output <dir>` | Output directory (defaults to `./<name>`) |

### Option 2 — the `dotnet new` template

```bash
dotnet new install FullStackHero.NET.StarterKit
dotnet new fsh -n MyApp
```

### Option 3 — clone the repository

```bash
git clone https://github.com/fullstackhero/dotnet-starter-kit.git MyApp && cd MyApp
dotnet run --project src/Host/FSH.Starter.AppHost
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) · [Docker](https://www.docker.com/) (Postgres / Valkey / MinIO via Aspire) · [Node.js 20+](https://nodejs.org/) (React apps). **No `aspire` workload is required** on .NET 10 — Aspire ships as NuGet packages.

### Default URLs & credentials (local dev via Aspire)

| Surface | URL |
|---------|-----|
| Aspire dashboard | `https://localhost:15888` |
| API + Scalar docs | `https://localhost:7030/scalar` |
| Admin console | `http://localhost:5173` |
| Tenant dashboard | `http://localhost:5174` |
| pgAdmin | `http://localhost:5050` |
| MinIO console | `http://localhost:9001` |
| RedisInsight | `http://localhost:5540` |

Sign in with a seeded account. Under Aspire the demo seeder runs, so **every seeded account (including the root admin) uses the password `Password123!`**:

- Root operator: `admin@root.com` / `Password123!` (tenant `root`, admin app)
- Demo tenant admins: `admin@acme.com`, `admin@globex.com` / `Password123!`
- Demo tenant users: e.g. `alice@acme.com` / `Password123!`

> In a production, `apply`-only deployment (no demo seed), the root admin password comes from `Seed:DefaultAdminPassword` (default `123Pa$$word!` in `appsettings.Development.json` / `SEED_ADMIN_PASSWORD` in Docker). **Rotate it immediately.**

---

## Architecture

- **Modular monolith / Vertical Slice Architecture.** Each module is a bounded context with a runtime project plus a `.Contracts` project that is its **only** public surface. Cross-module references go through `.Contracts` only — enforced by `Architecture.Tests` (NetArchTest).
- **CQRS via source-generated [Mediator](https://github.com/martinothamar/Mediator) 3.x.** Command/query handlers are `public sealed`, return `ValueTask<T>`, and propagate `CancellationToken` throughout.
- **Composition root** is `FSH.Starter.Api`; modules self-register through an `IModule` contract with assembly-level ordering metadata.
- **Database migrations never run at API startup.** A dedicated one-shot `DbMigrator` host applies migrations and seeds (the API waits for it under Aspire).
- **Tenant isolation is default-ON.** `BaseDbContext` applies the tenant filter automatically; opt out only via `IGlobalEntity`.

---

## Cross-cutting platform (BuildingBlocks)

The shared framework libraries under `src/BuildingBlocks` — yours to modify — provide:

- **Persistence** — EF Core 10 on PostgreSQL (Npgsql), tenant-isolated `DbContext` base, soft-delete + audit interceptors, domain events, and the specification pattern.
- **Web** — Minimal API conventions, global exception handling with **RFC 9457 `ProblemDetails`**, API versioning (`Asp.Versioning` 10), the SignalR `AppHub` real-time hub, and security middleware.
- **Caching** — **HybridCache** (`Microsoft.Extensions.Caching.Hybrid` 10.6) backed by **Valkey** (a BSD-licensed Redis fork), with stampede protection and structured cache keys.
- **Eventing** — in-process event bus with **Outbox/Inbox** scaffolding for idempotent cross-module handlers.
- **Storage** — pluggable blob storage (S3 / MinIO via AWS SDK, plus local), **presigned uploads**, and per-tenant quota enforcement.
- **Security** — CORS, security headers, **rate limiting**, **idempotency** filters, and request quotas.
- **Resilience** — outbound HTTP resilience via `Microsoft.Extensions.Http.Resilience` (Polly v8 pipelines).
- **Jobs** — **Hangfire** background and recurring jobs with an auth-gated dashboard.
- **Observability** — **Serilog** structured logging, **OpenTelemetry** traces/metrics/logs (OTLP → Aspire dashboard), correlation IDs, and `/health/live` + `/health/ready` probes.
- **Docs** — built-in **OpenAPI** with the **Scalar** reference UI; enums serialize as strings globally.

---

## Modules

Ten bounded contexts ship wired, migrated, seeded, and tested. ~171 HTTP endpoints across the API.

### Identity
JWT issuance & refresh on ASP.NET Identity; roles with **fine-grained permissions**; **group-based role assignment** (roles conferred via user groups grant permissions, not just JWT role claims); per-user permission caching with mutation-driven invalidation; rate-limited auth endpoints; password policies; user sessions; **impersonation**; self-registration and email confirmation; forgot/reset password with **anti-enumeration** responses; and a `GET /identity/permissions` endpoint that returns the caller's effective permission set for SPA gating.

### Multitenancy
Tenant resolution via **Finbuckle** 10 (JWT claim, with a root-operator header override applied post-authentication); full tenant **provisioning lifecycle** (Running/Completed/Failed) with per-tenant migrations and seeding; tenant `IsActive` / `ValidUpto` enforcement; per-tenant themes; and a **tenant billing lifecycle** (plan-driven subscriptions and invoices on create + renew, grace-windowed expiry).

### Billing
Plan-driven subscriptions and invoices, generated on tenant create and renewal; grace windows on expiry; root-operator cross-tenant administration with explicit tenant scoping on every handler (no cross-tenant leakage).

### Catalog
Reference commerce domain — **products with multiple images**, brands, and a category tree — demonstrating the full slice (contracts → handler → validator → endpoint → EF config → tests) and presigned image uploads.

### Tickets
Support ticketing with comments and a status lifecycle (open / update / close / delete), tenant-scoped and permission-gated.

### Chat
Real-time chat: named channels and **direct messages** (1:1 and group); messages with **threaded replies**, **emoji reactions**, **@mentions** (which raise notifications), edit/delete, and read markers; **full-text search** over Postgres `tsvector`; **typing indicators**; and **file attachments** governed by a channel-scoped file-access policy. Broadcast over SignalR with a Valkey backplane.

### Files
Presigned **S3 / MinIO** uploads with a request → finalize flow; **soft-delete + restore** (trash); public/private **visibility** control; pluggable per-owner-type file-access policies (e.g. product images, chat attachments); schema-per-tenant isolation.

### Webhooks
Outbound webhook **subscriptions** with **HMAC-signed** deliveries, fanout to subscribers, delivery tracking, test-send, and **encrypted subscription secrets**.

### Auditing
Append-only audit trail with dedicated **security**, **general**, and **exception** audit streams; `jsonb` payloads with filtered queries.

### Notifications
In-app notification inbox with unread counts, single and bulk mark-read (set-based `ExecuteUpdateAsync`), and SignalR push to `user:{id}` — the backbone for chat mentions and system alerts.

---

## Front-ends

Two independent React 19 SPAs, each with runtime configuration (`public/config.json`, no rebuild per environment), a hand-written typed `apiFetch` client (no codegen), and Playwright E2E suites.

### `clients/admin` — operator console
React 19 · Vite 7 · TypeScript 5.7 · TanStack Query v5 · React Router 7 · Radix + Tailwind v4 (shadcn-style) · `react-hook-form` + `zod` forms · SignalR. Per-route permission gating via `RouteGuard` mirrored against a permission catalog.

### `clients/dashboard` — tenant app
React 19 · Vite 7 · Tailwind v4 · TanStack Query v5 · SignalR + **SSE** live feed. Controlled-input forms (no RHF/zod). **Permissions are fetched** from `GET /api/v1/identity/permissions` (not read from the JWT, which carries only role names), cached with a `permissionsHydrated` flag; nav entries gate via `perm` / `anyPerm`. Includes the full real-time **chat** and **notifications** surfaces, an idle auto-logout, and a demo-account picker.

Both apps: idle → warning → auto sign-out, sonner toasts, React error boundaries on every route, and code-split lazy routes.

---

## Cloud-native & DevOps

- **.NET Aspire 13.4 AppHost** orchestrates the whole stack locally with one command: Postgres + pgAdmin, Valkey + RedisInsight, MinIO, the DbMigrator, a demo seeder, the API, and both React apps (via `AddJavaScriptApp`).
- **Docker Compose** production stack under `deploy/docker` (`fsh new` pre-generates a `.env` with strong secrets).
- **Terraform** for AWS under `deploy/terraform` (ECS Fargate, RDS, ElastiCache, S3/CloudFront).
- **SDK container publishing** (Dockerfile-less) for the API image; non-root, chiseled-friendly.
- **Path-scoped CI** — independent Backend (`src/**`) and Frontend (`clients/**`) pipelines; the two roll-up gate checks are the only required status checks.

---

## Testing & quality

- **Backend:** ~1,790 tests — xUnit, Shouldly, NSubstitute, AutoFixture; **NetArchTest** boundary/architecture rules; **Testcontainers** integration tests against real Postgres + MinIO. Run: `dotnet test src/FSH.Starter.slnx` (integration tests require Docker).
- **Frontend:** 214 Playwright E2E tests (admin 93 + dashboard 121), route-mocked and JWT-seeded. Run: `cd clients/<app> && npm run test:e2e`.
- **Build discipline:** file-scoped namespaces, explicit types, pattern matching, `TreatWarningsAsErrors`. Every command handler and paginated query has a validator (architecture-enforced).

---

## Dependency manifest (headline versions)

| Area | Package | Version |
|------|---------|---------|
| Runtime | .NET / TFM | `net10.0` |
| ORM | Microsoft.EntityFrameworkCore | 10.0.8 |
| Auth | Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.8 |
| Multitenancy | Finbuckle.MultiTenant | 10.1.0 |
| CQRS | Mediator (Abstractions / SourceGenerator) | 3.0.2 |
| Validation | FluentValidation | 12.1.1 |
| Caching | Microsoft.Extensions.Caching.Hybrid | 10.6.0 |
| Realtime | Microsoft.AspNetCore.SignalR.StackExchangeRedis | 10.0.8 |
| API versioning | Asp.Versioning.Http | 10.0.0 |
| Resilience | Microsoft.Extensions.Http.Resilience | 10.6.0 |
| Jobs | Hangfire | 1.8.23 |
| Storage | AWSSDK.S3 | 4.0.23.4 |
| Mail | MailKit | 4.17.0 |
| Logging | Serilog | 4.3.1 |
| Telemetry | OpenTelemetry (Extensions.Hosting) | 1.15.3 |
| API docs | Scalar.AspNetCore | 2.14.14 |
| Orchestration | Aspire.AppHost.Sdk | 13.4.0 |
| Integration tests | Testcontainers.PostgreSql | 4.11.0 |
| Frontend | React | 19 |
| Frontend | Vite | 7 |
| Frontend | TypeScript | 5.7 |
| Frontend | TanStack Query | 5 |
| Frontend | React Router | 7 |
| Frontend | Tailwind CSS | 4 |
| Frontend | @microsoft/signalr | 10 |

---

## Distribution & ownership model

- **You get source, not packages.** The template/CLI scaffold a complete, detached source tree. `FSH.Framework.*` and `FSH.Modules.*` namespaces stay fixed; only the product identity (`FSH.Starter.*`, brand strings, contact metadata) is renamed at scaffold time.
- **No per-module NuGet runtime.** `IsPackable=false` is the default across the solution; only the CLI (and, separately, the template content pack) are produced by `dotnet pack`.
- **License:** MIT — free and fully open source.

---

## Notes for this release

- **Aspire dashboard port** is pinned to `https://localhost:15888` (matches the CLI "Next Steps", `fsh doctor`, and the docs).
- **Package metadata:** both NuGet packages embed a 512×512 gallery icon and render the project README on nuget.org.
- **Generated package hygiene:** the template package ships **only git-tracked files** (an MSBuild `git ls-files` allowlist), so no local artifacts (`bin`/`obj`, `node_modules`, `.terraform`, `*.tfstate`, IDE/AI tool state) can leak into a scaffold.
- **Known flaky test:** one SignalR realtime integration test (`Chat/RealtimeEventsTests.SendingMessage_Should_Fire_ChatMessageCreated_To_Channel_Members`) is timing-sensitive under parallel load and passes on isolated rerun.

---

## Upgrade & migration

This is the inaugural public NuGet GA. Because scaffolded output is **detached, owned source**, updates are not a `dotnet pull`:

- The updatable surface is the framework layer (`src/BuildingBlocks`) and modules, whose `FSH.Framework.*` / `FSH.Modules.*` namespaces are stable across scaffolds — apply upstream changes by diffing against the corresponding release tag.
- Existing databases pick up schema changes via the DbMigrator: `dotnet run --project src/Host/<App>.DbMigrator -- apply` (add `--seed` to seed; `seed-demo` provisions demo tenants in Development only).

---

## Links

- **Documentation:** https://fullstackhero.net
- **Getting started:** https://fullstackhero.net/docs/getting-started/introduction/
- **Architecture:** https://fullstackhero.net/docs/architecture/
- **CLI reference:** https://fullstackhero.net/docs/cli/
- **Source & issues:** https://github.com/fullstackhero/dotnet-starter-kit
- **NuGet — CLI:** https://www.nuget.org/packages/FullStackHero.CLI
- **NuGet — Template:** https://www.nuget.org/packages/FullStackHero.NET.StarterKit

---

*Built and maintained by [Mukesh Murugan](https://codewithmukesh.com) and the FullStackHero community — for teams that want to ship fast without sacrificing architectural discipline.*
