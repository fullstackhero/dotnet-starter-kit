<div align="center">

# ⚡ FullStackHero .NET 10 Starter Kit

**A production-ready, modular .NET 10 monolith + two React 19 apps — the fastest way to ship a multi-tenant SaaS.**

Identity, multitenancy, billing, auditing, webhooks, files, chat, real-time, caching, jobs, storage, OpenAPI and OpenTelemetry — already wired, fully tested, and **100% yours as source** (no black-box packages).

[![fsh CLI](https://img.shields.io/nuget/v/FullStackHero.CLI?label=fsh%20cli&color=512BD4)](https://www.nuget.org/packages/FullStackHero.CLI)
[![template](https://img.shields.io/nuget/v/FullStackHero.NET.StarterKit?label=dotnet%20new%20fsh&color=512BD4)](https://www.nuget.org/packages/FullStackHero.NET.StarterKit)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com/download)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Docs](https://img.shields.io/badge/docs-fullstackhero.net-2563eb)](https://fullstackhero.net)
[![Stars](https://img.shields.io/github/stars/fullstackhero/dotnet-starter-kit?style=social)](https://github.com/fullstackhero/dotnet-starter-kit)

### [📖 Documentation](https://fullstackhero.net) · [🚀 Get Started](https://fullstackhero.net/docs/getting-started/introduction/) · [🧩 Modules](https://fullstackhero.net/docs/modules/) · [🏗️ Architecture](https://fullstackhero.net/docs/architecture/) · [📦 Changelog](https://fullstackhero.net/docs/changelog/)

</div>

---

## Why FullStackHero?

Most starter kits give you a login page and a TODO list. This one gives you the **boring, hard parts already done right** — multitenancy, auth, billing, auditing, background jobs, real-time, file storage, observability — across a clean **Vertical Slice** backend *and* two polished **React 19** front-ends, orchestrated locally with one command via **.NET Aspire**, and deployable to Docker or AWS.

You scaffold with the `fsh` CLI and get the **complete, detached source** — every BuildingBlock, Module, and Host project with real project references. No hidden NuGet runtime, nothing to "eject" later. Own it, read it, change it.

```bash
dotnet tool install -g FullStackHero.CLI
fsh new MyApp
cd MyApp
dotnet run --project src/Host/MyApp.AppHost   # 🎉 whole stack up: API + 2 React apps + Postgres + Valkey + MinIO
```

> Then open the **Aspire dashboard** at `https://localhost:15888`, the **API + Scalar docs** at `https://localhost:7030/scalar`, the **admin** app at `http://localhost:5173`, and the **dashboard** app at `http://localhost:5174`. Sign in with a seeded demo account (e.g. `admin@acme.com` / `Password123!`).

---

## ✨ What's inside

### Backend — modular monolith, vertical slices
- **.NET 10 · C# latest · Minimal APIs · [Mediator](https://github.com/martinothamar/Mediator) (source-generated CQRS) · FluentValidation**
- **EF Core 10** on **PostgreSQL** (Npgsql), with domain events, the specification pattern, soft-delete + audit interceptors, and tenant-isolated `DbContext`s.
- **JWT auth + ASP.NET Identity** — issuance/refresh, roles & fine-grained permissions, rate-limited auth, password policies, sessions, impersonation.
- **Multitenancy** via [Finbuckle](https://www.finbuckle.com/) — tenant resolution, provisioning, per-tenant migrations & seeding, isolation enforced by default.
- **Cross-cutting**: HybridCache on **Valkey** (Redis-compatible), **Hangfire** jobs, presigned S3/**MinIO** storage, mailing, idempotency, quotas, rate limiting, API versioning, RFC 9457 `ProblemDetails`.
- **Observability**: Serilog structured logging + **OpenTelemetry** traces/metrics/logs, health probes, security/exception auditing.
- **Docs**: **OpenAPI** + the **Scalar** API reference UI.

### Front-ends — two React 19 apps
- **`clients/admin`** (operator console) and **`clients/dashboard`** (tenant app): **React 19 + Vite 7 + TypeScript**, **TanStack Query v5**, **React Router 7**, **Radix + Tailwind v4** (shadcn-style), real-time via **SignalR**/**SSE**.
- Runtime config (`/config.json`, no rebuild per environment), hand-written typed API client, and **Playwright** E2E suites.

### Modules (bounded contexts)
**Identity · Multitenancy · Billing · Catalog · Tickets · Chat · Files · Webhooks · Auditing · Notifications** — each a runtime project plus a `.Contracts` project (its only public surface), boundaries enforced by architecture tests.

### Cloud-native & DevOps
- **.NET Aspire** orchestrates the entire stack locally with one command (Postgres + pgAdmin, Valkey + RedisInsight, MinIO, migrator, demo-seeder, API, and both React apps).
- **Docker Compose** production stack (`deploy/docker`) and **Terraform** for AWS (`deploy/terraform`); API image published to GHCR.
- A one-shot **DbMigrator** (migrations are never run at API startup), and the **`fsh` CLI** + `dotnet new` template for distribution.

### Quality
**1,600+ backend tests** (xUnit, Shouldly, NSubstitute, AutoFixture, **NetArchTest** boundaries, **Testcontainers** integration) and **200+ front-end E2E tests** (Playwright). Path-scoped CI for backend and frontend; warnings-as-errors.

---

## 🚀 Getting started

### Option 1 — the `fsh` CLI (recommended)

```bash
dotnet tool install -g FullStackHero.CLI
fsh doctor          # verify your environment (SDK, Docker, Aspire, ports)
fsh new MyApp       # interactive wizard
```

The wizard asks what to include (Aspire AppHost, the React apps). Non-interactive:

```bash
fsh new MyApp --non-interactive          # full stack, Postgres
fsh new MyApp --no-frontend              # backend-only
fsh new MyApp --no-aspire --no-frontend  # minimal API + migrator
```

### Option 2 — the `dotnet new` template

```bash
dotnet new install FullStackHero.NET.StarterKit
dotnet new fsh -n MyApp
```

### Option 3 — clone the repo

```bash
git clone https://github.com/fullstackhero/dotnet-starter-kit.git MyApp && cd MyApp
dotnet run --project src/Host/FSH.Starter.AppHost
```

> **Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) · [Docker](https://www.docker.com/) (Postgres/Valkey/MinIO via Aspire) · [Node 20+](https://nodejs.org/) (for the React apps).

**`fsh` commands:** `new` · `doctor` · `info` · `update` · `--version`. Full reference → [fullstackhero.net/docs/cli](https://fullstackhero.net/docs/cli/).

---

## 🧱 Tech stack

| Backend | | Frontend | |
|---|---|---|---|
| Runtime | .NET 10 / C# latest | Framework | React 19 + Vite 7 + TS 5 |
| API | Minimal APIs + Mediator (CQRS) | Data | TanStack Query v5 |
| Validation | FluentValidation | Routing | React Router 7 |
| ORM / DB | EF Core 10 / PostgreSQL | UI | Radix + Tailwind v4 (shadcn) |
| Auth | JWT + ASP.NET Identity | Realtime | SignalR · SSE |
| Multitenancy | Finbuckle 10 | Tests | Playwright |
| Cache / Jobs | Valkey · Hangfire | | |
| Storage | S3 / MinIO (presigned) | **Infra** | |
| Docs | OpenAPI + Scalar | Orchestration | .NET Aspire |
| Observability | Serilog + OpenTelemetry | Deploy | Docker Compose · Terraform |
| Testing | xUnit · Testcontainers · NetArchTest | | |

---

## 🗺️ Repository layout

| Path | What |
|---|---|
| `src/BuildingBlocks/` | Shared framework libraries (Core, Persistence, Web, Caching, Eventing, Storage, Quota…) |
| `src/Modules/{Name}/` | Bounded contexts — each with a runtime project + a `.Contracts` project (its public API) |
| `src/Host/FSH.Starter.Api` | Composition-root Web API host |
| `src/Host/FSH.Starter.AppHost` | .NET Aspire orchestrator (Postgres, Valkey, MinIO, migrator, API, both React apps) |
| `src/Host/FSH.Starter.DbMigrator` | One-shot migrate/seed runner (DB is **not** migrated at API startup) |
| `src/Tools/CLI` | The `fsh` CLI (Spectre.Console) |
| `clients/admin`, `clients/dashboard` | The two React apps |
| `deploy/` | Docker Compose, Terraform (AWS), Dokploy |
| `src/Tests/` | Unit, Architecture (NetArchTest), Integration (Testcontainers) |

Architecture deep-dive → [fullstackhero.net/docs/architecture](https://fullstackhero.net/docs/architecture/).

---

## ☁️ Deploy

**Single-host via Docker Compose:**

```bash
cd deploy/docker
cp .env.example .env   # fsh new pre-generates this with strong secrets
docker compose up -d --build
```

**AWS via Terraform** (ECS Fargate + RDS + ElastiCache + S3/CloudFront) lives in `deploy/terraform`.

Guides → [Local orchestration](https://fullstackhero.net/docs/deployment/aspire/) · [Docker](https://fullstackhero.net/docs/deployment/) · [AWS / Terraform](https://fullstackhero.net/docs/deployment/aws-terraform/) · [Database migrations](https://fullstackhero.net/docs/deployment/database-migrations/).

---

## 🧪 Testing

```bash
dotnet test src/FSH.Starter.slnx        # backend: unit + architecture + Testcontainers integration
cd clients/admin     && npm run test:e2e # Playwright (operator app)
cd clients/dashboard && npm run test:e2e # Playwright (tenant app)
```

> Integration tests require Docker (Testcontainers spins real Postgres). Architecture tests enforce module boundaries.

---

## 📖 Documentation

Full guides, module references, and architecture decisions live at **[fullstackhero.net](https://fullstackhero.net)**:

- [Getting started](https://fullstackhero.net/docs/getting-started/introduction/) — scaffold, run, and the default credentials
- [Architecture](https://fullstackhero.net/docs/architecture/) — modular monolith + vertical slices, multitenancy deep-dive
- [Modules](https://fullstackhero.net/docs/modules/) — Identity, Catalog, Tickets, Chat, and more
- [Local orchestration with Aspire](https://fullstackhero.net/docs/deployment/aspire/)
- [CLI reference](https://fullstackhero.net/docs/cli/) · [Changelog](https://fullstackhero.net/docs/changelog/)

---

## 🤝 Contributing

Issues and PRs are welcome — see [`CONTRIBUTING.md`](CONTRIBUTING.md). Branch from and target **`main`**; CI runs path-scoped backend + frontend pipelines, and stable releases are cut from `v*` tags.

## 📄 License

MIT — see [`LICENSE`](LICENSE). Built and maintained by [**Mukesh Murugan**](https://codewithmukesh.com) and the FullStackHero community, for teams that want to ship fast without sacrificing architectural discipline.

<div align="center">

**[⭐ Star us on GitHub](https://github.com/fullstackhero/dotnet-starter-kit)** if this saves you time — it genuinely helps.

</div>
