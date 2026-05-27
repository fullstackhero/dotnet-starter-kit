# FSH.Starter

Your application, generated from the **FSH .NET Starter Kit** — a production-ready modular
.NET 10 monolith with two React 19 apps, multitenancy, identity, background jobs, and
cloud-native deploy.

You **own all of this source**. There are no framework NuGet packages to track or upgrade —
the shared code lives in `src/BuildingBlocks` and is yours to change.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 20+](https://nodejs.org) — for the React apps
- [Docker](https://www.docker.com/) — Postgres, Redis, MinIO (orchestrated by Aspire)
- .NET Aspire workload: `dotnet workload install aspire`

## Quick start

### Everything at once (recommended) — .NET Aspire

```bash
dotnet run --project src/Host/FSH.Starter.AppHost
```

Aspire starts Postgres, Redis, and MinIO, runs database migrations, then launches the API
**and both React apps**.

| Surface | URL |
|---|---|
| Aspire dashboard | https://localhost:15888 |
| API + Scalar docs | https://localhost:7030/scalar |
| Admin console | http://localhost:5173 |
| Tenant dashboard | http://localhost:5174 |

### Backend only

```bash
dotnet run --project src/Host/FSH.Starter.Api      # needs external Postgres + Redis
```

### Frontend only (against a running API)

```bash
cd clients/admin     && npm install && npm run dev   # → http://localhost:5173
cd clients/dashboard && npm install && npm run dev   # → http://localhost:5174
```

The React apps read their API URL at runtime from `public/config.json` — no rebuild to repoint.

## Project structure

```
src/
  BuildingBlocks/      Shared framework libraries — yours to modify
  Modules/             Bounded contexts: Identity, Multitenancy, Auditing, Billing,
                       Catalog, Chat, Files, Notifications, Tickets, Webhooks
  Host/
    FSH.Starter.Api/                    API composition root
    FSH.Starter.AppHost/                .NET Aspire orchestrator
    FSH.Starter.DbMigrator/             One-shot migrate / seed runner
    FSH.Starter.Migrations.PostgreSQL/  EF Core migrations
  Tests/               Unit, integration (Testcontainers), and architecture tests
clients/
  admin/               Operator console (React 19 + Vite + Tailwind)
  dashboard/           Tenant app (React 19 + Vite + Tailwind, SSE live feed)
deploy/
  docker/              Production docker-compose + .env
  terraform/           AWS infrastructure (ECS, RDS, ElastiCache, S3)
```

## Database

Migrations run automatically under Aspire. To apply them yourself:

```bash
dotnet run --project src/Host/FSH.Starter.DbMigrator -- apply --seed
```

## Make it yours — first-run checklist

This project shipped with sensible defaults. Before production:

- [ ] **Secrets** — set strong values in `deploy/docker/.env` (the `fsh` CLI generates these
      for you; otherwise `cp deploy/docker/.env.example deploy/docker/.env` and fill them in).
      Never commit `.env`.
- [ ] **Logo** — replace `clients/admin/public/logo-fullstackhero.png` and
      `clients/dashboard/public/logo-fullstackhero.png` with your own.
- [ ] **Mail** — configure SMTP / SendGrid under `MailOptions` in
      `src/Host/FSH.Starter.Api/appsettings.json`.
- [ ] **OpenAPI contact** — update `OpenApiOptions.Contact` in `appsettings.json`.
- [ ] **Container registry & infra** — set your registry and review bucket / database names
      in `deploy/terraform/apps/starter/**/variables.tf` and `terraform.tfvars`.

## Production (Docker Compose)

```bash
cd deploy/docker
# .env is generated for you by the fsh CLI; otherwise: cp .env.example .env && edit
docker compose up -d --build
```

Sign in to the admin console as `admin@root.com` using the `SEED_ADMIN_PASSWORD` from your
`.env`, then rotate it from Settings → Security.

## Adding a feature

1. Contracts command/query in `src/Modules/{Module}.Contracts/v1/{Area}/{Feature}/`
2. Handler + FluentValidation validator in `src/Modules/{Module}/Features/...`
3. Endpoint, wired into the module's `MapEndpoints()`
4. Tests

## Running tests

```bash
dotnet test src/FSH.Starter.slnx       # integration tests require Docker
```

## Learn more

- [FSH Documentation](https://fullstackhero.net)
- [Source & issues](https://github.com/fullstackhero/dotnet-starter-kit)
