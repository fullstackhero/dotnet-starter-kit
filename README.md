# FullStackHero .NET 10 Starter Kit

[![NuGet](https://img.shields.io/nuget/v/FullStackHero.CLI?label=fsh%20cli)](https://www.nuget.org/packages/FullStackHero.CLI)
[![NuGet](https://img.shields.io/nuget/v/FullStackHero.Framework.Web?label=framework)](https://www.nuget.org/packages/FullStackHero.Framework.Web)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

An opinionated, production-first starter for building multi-tenant SaaS and enterprise APIs on .NET 10. You get ready-to-ship Identity, Multitenancy, Auditing, Webhooks, caching, mailing, jobs, storage, health, OpenAPI, and OpenTelemetry — wired through Minimal APIs, Mediator, and EF Core.

## Quick Start

You get the complete source code — BuildingBlocks, Modules, and Host — with full project references. No black-box NuGet packages; you own and can modify everything.

### Option 1: FSH CLI (recommended)

```bash
dotnet tool install -g FullStackHero.CLI
fsh new MyApp
cd MyApp
dotnet run --project src/Host/MyApp.AppHost
```

The interactive wizard lets you pick your database provider and whether to include Aspire. Run `fsh doctor` to verify your environment first.

### Option 2: dotnet new template

```bash
dotnet new install FullStackHero.NET.StarterKit
dotnet new fsh -n MyApp
cd MyApp
dotnet run --project src/Host/MyApp.AppHost
```

### Option 3: Clone the repository

```bash
git clone https://github.com/fullstackhero/dotnet-starter-kit.git MyApp
cd MyApp
dotnet restore src/FSH.Starter.slnx
dotnet run --project src/Host/FSH.Starter.AppHost
```

### Option 4: GitHub Codespaces

Click **"Use this template"** on GitHub, or open in Codespaces for a zero-install experience with .NET 10, Docker, and Aspire pre-configured.

> Prerequisites: [.NET 10 SDK](https://dotnet.microsoft.com/download), [Docker](https://www.docker.com/) (for Postgres/Redis via Aspire)

## Deploy

Production-style single-host deployment via Docker Compose:

```bash
cd deploy/docker
cp .env.example .env && $EDITOR .env
docker compose up -d --build
```

Full walkthrough — prereqs, external proxy wiring, backup, swapping to managed services — in [`deploy/docker/README.md`](deploy/docker/README.md).

## FSH CLI Commands

| Command | Description |
|---------|------------|
| `fsh new [name]` | Create a new project with interactive wizard |
| `fsh doctor` | Check your environment (SDK, Docker, Aspire, ports) |
| `fsh info` | Show CLI/template versions and available updates |
| `fsh update` | Update CLI tool and template to latest |

```bash
# Non-interactive with options
fsh new MyApp --db sqlserver --no-aspire --no-git

# Dry run (preview without creating)
fsh new MyApp --dry-run
```

## Why teams pick this
- Modular vertical slices: drop `Modules.Identity`, `Modules.Multitenancy`, `Modules.Auditing`, `Modules.Webhooks` into any API and let the module loader wire endpoints.
- Battle-tested building blocks: persistence + specifications, distributed caching, mailing, jobs via Hangfire, storage abstractions, and web host primitives (auth, rate limiting, versioning, CORS, exception handling).
- Cloud-ready out of the box: Aspire AppHost spins up Postgres + Redis + the API host with OTLP tracing enabled.
- Multi-tenant from day one: Finbuckle-powered tenancy across Identity and your module DbContexts; helpers to migrate and seed tenant databases on startup.
- Observability baked in: OpenTelemetry traces/metrics/logs, structured logging, health checks, and security/exception auditing.

## Stack highlights
- .NET 10, C# latest, Minimal APIs, Mediator for commands/queries, FluentValidation.
- EF Core 10 with domain events + specifications; Postgres by default, SQL Server ready.
- ASP.NET Identity with JWT issuance/refresh, roles/permissions, rate-limited auth endpoints.
- Hangfire for background jobs; Redis-backed distributed cache; pluggable storage.
- API versioning, rate limiting, CORS, security headers, OpenAPI (Swagger) + Scalar docs.

## Repository map
- `src/BuildingBlocks` — Core abstractions (DDD primitives, exceptions), Persistence, Caching, Mailing, Jobs, Storage, Web host wiring.
- `src/Modules` — `Identity`, `Multitenancy`, `Auditing`, `Webhooks` runtime + contracts projects.
- `src/Host` — Composition-root host (`FSH.Starter.Api`), Aspire app host (`FSH.Starter.AppHost`), Postgres migrations.
- `src/Tools/CLI` — The `fsh` CLI tool source code.
- `src/Tests` — Architecture tests that enforce layering and module boundaries.
- `deploy` — Docker, Dokploy, and Terraform deployment scaffolding.

## Run it now (Aspire)
Prereqs: .NET 10 SDK, Aspire workload, Docker running (for Postgres/Redis).

1. Restore: `dotnet restore src/FSH.Starter.slnx`
2. Start everything: `dotnet run --project src/Host/FSH.Starter.AppHost`
   - Aspire brings up Postgres + Redis containers, wires env vars, launches the API host, and enables OTLP export on https://localhost:4317.
3. Hit the API: `https://localhost:5285` (Swagger/Scalar and module endpoints under `/api/v1/...`).

### Run the API only
- Set env vars or appsettings for `DatabaseOptions__Provider`, `DatabaseOptions__ConnectionString`, `DatabaseOptions__MigrationsAssembly`, `CachingOptions__Redis`, and JWT options.
- Run: `dotnet run --project src/Host/FSH.Starter.Api`
- The host applies migrations/seeding via `UseHeroMultiTenantDatabases()` and maps module endpoints via `UseHeroPlatform`.

## Bring the framework into your API
- Reference the building block and module projects you need.
- In `Program.cs`:
  - Register Mediator with assemblies containing your commands/queries and module handlers.
  - Call `builder.AddHeroPlatform(...)` to enable auth, OpenAPI, caching, mailing, jobs, health, OTel, rate limiting.
  - Call `builder.AddModules(moduleAssemblies)` and `app.UseHeroPlatform(p => p.MapModules = true);`.
- Configure connection strings, Redis, JWT, CORS, and OTel endpoints via configuration. Example wiring lives in `src/Host/FSH.Starter.Api/Program.cs`.

## Included modules
- **Identity** — ASP.NET Identity + JWT issuance/refresh, user/role/permission management, profile image storage, login/refresh auditing, health checks.
- **Multitenancy** — Tenant provisioning, migrations, status/upgrade APIs, tenant-aware EF Core contexts, health checks.
- **Auditing** — Security/exception/activity auditing with queryable endpoints; plugs into global exception handling and Identity events.
- **Webhooks** — Tenant-scoped webhook subscriptions with HMAC-signed delivery, retry policies, and delivery logs.

## Development notes
- Target framework: `net10.0`; nullable enabled; analyzers on.
- Tests: `dotnet test src/FSH.Starter.slnx` (includes architecture guardrails).
- Want the deeper story? Browse the docs site under `docs/` (Astro Starlight) — start with [`docs/src/content/docs/architecture.mdx`](docs/src/content/docs/architecture.mdx), [`project-structure.mdx`](docs/src/content/docs/project-structure.mdx), and the [`adding-a-feature.mdx`](docs/src/content/docs/adding-a-feature.mdx) walkthrough. Run `cd docs && npm install && npm run dev` to read it locally.

Built and maintained by Mukesh Murugan for teams that want to ship faster without sacrificing architecture discipline.
