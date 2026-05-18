# Production Docker Deploy — Design

**Date:** 2026-05-19
**Status:** Approved
**Scope:** v1 punchlist — Infra section

## Goals

Give operators a canonical, repeatable way to deploy the starter kit on a
single host with all dependencies inside `docker compose`. Make it the
"this is how you ship FSH" answer the README points at.

Specifically:

- Frontend Dockerfiles for `clients/admin` and `clients/dashboard` that
  produce one image per app, configurable per-deploy via runtime env.
- A production `docker-compose.yml` at `deploy/docker/` that brings up
  the full stack (API, DbMigrator one-shot, admin, dashboard, Postgres,
  Redis, MinIO) on a single host.
- A `.env.example` at `deploy/docker/` documenting every knob the
  operator must (or may) set.
- Container hardening on the .NET host projects: drop root via
  `$APP_UID`, swap the runtime base to `noble-chiseled`.

## Non-goals

- Reverse proxy / TLS termination inside compose. The operator owns the
  edge (Cloudflare Tunnel / ALB / their own nginx / Tailscale Funnel /
  whatever). Compose publishes raw ports.
- Helm chart, k8s manifests, Azure Container Apps `azd` template. These
  remain v1.1 work.
- Multi-arch (linux/arm64) images. Tracked separately on the CI side as
  a P1 publish-workflow change; not in this PR.
- A "localhost demo" mode. Aspire (AppHost) remains the local dev
  story; this compose targets real deployments.

## Decisions locked during brainstorming (2026-05-19)

- **Deploy target:** single-host, everything inside compose. Operator
  swaps Postgres/Redis/MinIO for managed services by editing compose
  later.
- **Routing:** subdomains (`api.example.com`, `app.example.com`,
  `admin.example.com`). Each frontend gets its own origin; CORS is
  explicit; cookies can share the eTLD+1 if needed.
- **Frontend config:** runtime `/config.json` rendered from env at
  container start, fetched by the app before React mounts. Single image
  works for any deploy.
- **Reverse proxy:** none in compose. Services publish ports; the
  operator's external proxy handles TLS + subdomain routing.
- **Image source:** build from source in compose
  (`build: ./src/...` / `./clients/...`). No GHCR-pull dependency.
  Update = `git pull && docker compose up -d --build`.
- **Hardening bundled:** `ContainerUser=$APP_UID` +
  `ContainerFamily=noble-chiseled` on API and DbMigrator csprojs.
- **.env location:** single `deploy/docker/.env.example`. No
  repo-root env file (it would have to serve too many masters —
  dotnet user-secrets, docker, future k8s).

## Architecture

```
External proxy (operator-owned)
        │
   ┌────┴─────┬──────────────┬───────────────┐
   │          │              │               │
:8080      :8081           :8082          (your TLS termination here)
   │          │              │
   ▼          ▼              ▼
 api      admin          dashboard
 (kestrel)(nginx)        (nginx)
   │
   ├──── depends_on (service_completed_successfully) ────► migrator (one-shot)
   │
   ▼
 postgres  redis  minio   (compose-internal, no published ports)
```

Apps and frontends live on the default compose bridge network. The
data plane (`postgres`, `redis`, `minio`) is reachable by hostname only;
no host ports are published unless the operator explicitly opts in.

DbMigrator runs once on `docker compose up`. It applies pending
migrations and seeds the root tenant + default admin user using
`Seed:DefaultAdminPassword` from the env. `api`, `admin`, and
`dashboard` all gate on `migrator: { condition: service_completed_successfully }`
so they never start against an unmigrated database.

## Artifact layout

```
clients/admin/
├── Dockerfile                          # multi-stage: vite build → nginx serve
└── docker/
    ├── nginx.conf                      # SPA fallback + security headers
    ├── config.json.template            # `{ "apiUrl": "${FSH_API_URL}" }`
    └── docker-entrypoint.sh            # envsubst + exec nginx

clients/dashboard/                      # same shape
├── Dockerfile
└── docker/
    ├── nginx.conf
    ├── config.json.template
    └── docker-entrypoint.sh

src/Host/FSH.Starter.Api/
├── Dockerfile                          # multi-stage: sdk build → aspnet-chiseled runtime
└── FSH.Starter.Api.csproj              # +ContainerUser, +ContainerFamily

src/Host/FSH.Starter.DbMigrator/
├── Dockerfile                          # same shape; runtime stage = aspnet-chiseled
└── FSH.Starter.DbMigrator.csproj       # +ContainerUser, +ContainerFamily

deploy/docker/
├── docker-compose.yml
├── .env.example
├── README.md
└── postgres-init/
    └── 01-create-databases.sql         # CREATE DATABASE fsh; CREATE EXTENSION ...

.dockerignore                           # bin/, obj/, node_modules/, clients/*/dist excluded from all build contexts
```

## Component specs

### Frontend Dockerfile (one shape, two instances)

Multi-stage build.

**Stage 1 (`node:22-alpine`)** — install + build:

```dockerfile
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build
```

The build emits hashed asset bundles to `dist/`. The bundle uses
`fetch('/config.json')` before React mounts (already established
behaviour — covered by the existing app code path that previously read
`VITE_API_URL`; the refactor to fetch-then-mount is part of this PR).

**Stage 2 (`nginx:alpine`)** — serve:

```dockerfile
WORKDIR /usr/share/nginx/html
COPY --from=build /app/dist/ ./
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY docker/config.json.template /usr/share/nginx/html/config.json.template
COPY docker/docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh
EXPOSE 80
ENTRYPOINT ["/docker-entrypoint.sh"]
```

**`docker-entrypoint.sh`** renders the runtime config and execs nginx:

```bash
#!/bin/sh
set -e
: "${FSH_API_URL:?FSH_API_URL is required}"
envsubst < /usr/share/nginx/html/config.json.template \
        > /usr/share/nginx/html/config.json
exec nginx -g 'daemon off;'
```

**`nginx.conf`** — SPA fallback, asset caching, security headers, no
caching on `config.json`:

```nginx
server {
  listen 80;
  root /usr/share/nginx/html;
  index index.html;

  # immutable hashed assets — long-lived
  location ~* \.(?:js|css|woff2?|png|jpg|svg|ico)$ {
    expires 1y;
    add_header Cache-Control "public, immutable";
  }

  # runtime config — must be re-fetched on every deploy
  location = /config.json {
    add_header Cache-Control "no-store";
  }

  # SPA fallback
  location / {
    try_files $uri /index.html;
    add_header X-Frame-Options "DENY";
    add_header X-Content-Type-Options "nosniff";
    add_header Referrer-Policy "strict-origin-when-cross-origin";
  }
}
```

**`config.json.template`** — literally `{ "apiUrl": "${FSH_API_URL}" }`.

### App boot refactor (admin + dashboard)

Today the apps read `import.meta.env.VITE_API_URL` at build time. To
support runtime config, replace that read site with a one-time fetch
before React mounts:

```ts
// src/main.tsx
const cfg = await fetch('/config.json').then(r => r.json());
window.__FSH_CFG = cfg;            // typed via a declare module
// ... then mount React, env.ts re-exports window.__FSH_CFG.apiUrl
```

`src/env.ts` becomes a thin accessor over `window.__FSH_CFG`. Dev
mode (vite) ships a static `public/config.json` with the dev API URL
so the same code path works locally.

### `docker-compose.yml`

```yaml
name: fsh

services:
  postgres:
    image: postgres:17-alpine
    environment:
      POSTGRES_DB: fsh
      POSTGRES_USER: fsh
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:?POSTGRES_PASSWORD required}
    volumes:
      - pg_data:/var/lib/postgresql/data
      - ./postgres-init:/docker-entrypoint-initdb.d:ro
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U fsh"]
      interval: 5s
      timeout: 3s
      retries: 12

  redis:
    image: redis:7-alpine
    command: ["redis-server", "--requirepass", "${REDIS_PASSWORD:?REDIS_PASSWORD required}", "--appendonly", "yes"]
    volumes:
      - redis_data:/data
    healthcheck:
      test: ["CMD", "redis-cli", "-a", "${REDIS_PASSWORD}", "PING"]
      interval: 5s
      timeout: 3s
      retries: 12

  minio:
    image: minio/minio:latest
    command: ["server", "/data", "--console-address", ":9001"]
    environment:
      MINIO_ROOT_USER: ${MINIO_ROOT_USER:?MINIO_ROOT_USER required}
      MINIO_ROOT_PASSWORD: ${MINIO_ROOT_PASSWORD:?MINIO_ROOT_PASSWORD required}
    volumes:
      - minio_data:/data
    healthcheck:
      test: ["CMD", "mc", "ready", "local"]
      interval: 10s
      timeout: 3s
      retries: 12

  migrator:
    build:
      context: ../..
      dockerfile: src/Host/FSH.Starter.DbMigrator/Dockerfile
    image: fsh/dbmigrator:local
    depends_on:
      postgres: { condition: service_healthy }
    environment:
      DOTNET_ENVIRONMENT: Production
      DatabaseOptions__ConnectionString: Host=postgres;Database=fsh;Username=fsh;Password=${POSTGRES_PASSWORD}
      CachingOptions__Redis: redis,password=${REDIS_PASSWORD}
      JwtOptions__SigningKey: ${JWT_SIGNING_KEY:?JWT_SIGNING_KEY required}
      Seed__DefaultAdminPassword: ${SEED_ADMIN_PASSWORD:?SEED_ADMIN_PASSWORD required}
    restart: "no"
    command: ["apply"]

  api:
    build:
      context: ../..
      dockerfile: src/Host/FSH.Starter.Api/Dockerfile
    image: fsh/api:local
    depends_on:
      migrator: { condition: service_completed_successfully }
      redis:    { condition: service_healthy }
      minio:    { condition: service_healthy }
    environment:
      ASPNETCORE_URLS: http://+:8080
      DOTNET_ENVIRONMENT: Production
      DatabaseOptions__ConnectionString: Host=postgres;Database=fsh;Username=fsh;Password=${POSTGRES_PASSWORD}
      CachingOptions__Redis: redis,password=${REDIS_PASSWORD}
      JwtOptions__SigningKey: ${JWT_SIGNING_KEY}
      Seed__DefaultAdminPassword: ${SEED_ADMIN_PASSWORD}
      Storage__Provider: s3
      Storage__S3__ServiceUrl: http://minio:9000
      Storage__S3__AccessKey: ${MINIO_ROOT_USER}
      Storage__S3__SecretKey: ${MINIO_ROOT_PASSWORD}
      Storage__S3__Bucket: fsh
      Storage__S3__ForcePathStyle: "true"
      CorsOptions__AllowedOrigins__0: ${FSH_ADMIN_URL:?FSH_ADMIN_URL required}
      CorsOptions__AllowedOrigins__1: ${FSH_DASHBOARD_URL:?FSH_DASHBOARD_URL required}
      OpenTelemetryOptions__Exporter__Otlp__Endpoint: ${OTEL_EXPORTER_OTLP_ENDPOINT:-}
    ports:
      - "${FSH_API_PORT:-8080}:8080"

  admin:
    build:
      context: ../../clients/admin
    image: fsh/admin:local
    depends_on:
      migrator: { condition: service_completed_successfully }
    environment:
      FSH_API_URL: ${FSH_API_URL:?FSH_API_URL required}
    ports:
      - "${FSH_ADMIN_PORT:-8081}:80"

  dashboard:
    build:
      context: ../../clients/dashboard
    image: fsh/dashboard:local
    depends_on:
      migrator: { condition: service_completed_successfully }
    environment:
      FSH_API_URL: ${FSH_API_URL}
    ports:
      - "${FSH_DASHBOARD_PORT:-8082}:80"

volumes:
  pg_data:
  redis_data:
  minio_data:
```

Notes:

- `:?` env-var syntax fails-fast at compose-parse time if a required
  value is missing. No silently-empty deploys.
- `build: context: ../..` is relative to `deploy/docker/`. The
  `.dockerignore` at the repo root excludes `bin/`, `obj/`,
  `node_modules/`, `clients/*/dist`, etc. so the build context stays
  small.
- The .NET host projects ship a hand-written multi-stage Dockerfile
  (build stage `mcr.microsoft.com/dotnet/sdk:10.0`, runtime stage
  `mcr.microsoft.com/dotnet/nightly/aspnet:10.0-noble-chiseled`).
  We do NOT call the SDK's `PublishContainer` target from compose —
  that target is great in CI where it pushes to GHCR, but it doesn't
  produce a Dockerfile compose can `build:` against. The two flows
  coexist: CI uses `PublishContainer` for GHCR; compose uses the
  Dockerfile for local builds. Both produce equivalent runtime
  images thanks to the `<ContainerFamily>` and `<ContainerUser>` csproj
  settings being respected by both.

### `.env.example`

```dotenv
# ──────────────────────────────────────────────────────────────────────
# FullStackHero — production docker-compose configuration.
# Copy to `.env` and edit. NEVER commit the .env file.
# ──────────────────────────────────────────────────────────────────────

# ── Public URLs ──────────────────────────────────────────────────────
# Where the API will be reachable from end users' browsers (via your
# external proxy / TLS terminator). Used by:
#   • frontends, baked into /config.json at container start
#   • API CORS allow-list for the two frontend origins
FSH_API_URL=https://api.example.com
FSH_ADMIN_URL=https://admin.example.com
FSH_DASHBOARD_URL=https://app.example.com

# ── Host ports for the external proxy to point at ───────────────────
# Only the FSH services publish ports. Postgres/Redis/MinIO stay
# compose-internal by default.
FSH_API_PORT=8080
FSH_ADMIN_PORT=8081
FSH_DASHBOARD_PORT=8082

# ── Secrets ─────────────────────────────────────────────────────────
# JWT signing key — 32+ chars. Generate with:
#   openssl rand -base64 48
JWT_SIGNING_KEY=

# Initial root admin password used by the seeder on first boot.
# Sign in once at https://admin.example.com with admin@root.com,
# rotate immediately. Required (no default after the v1 hardening).
SEED_ADMIN_PASSWORD=

# ── Data plane (defaults are fine for self-hosted compose) ──────────
POSTGRES_PASSWORD=
REDIS_PASSWORD=
MINIO_ROOT_USER=
MINIO_ROOT_PASSWORD=

# ── Observability (optional) ────────────────────────────────────────
# Point at an OTLP collector. Leave blank to disable export.
OTEL_EXPORTER_OTLP_ENDPOINT=
```

### Container hardening (API + DbMigrator csprojs)

```xml
<PropertyGroup>
  ...
  <ContainerUser>$APP_UID</ContainerUser>
  <ContainerFamily>noble-chiseled</ContainerFamily>
</PropertyGroup>
```

- `$APP_UID` is the `1654` uid Microsoft bakes into modern aspnet base
  images. Running as that user drops root inside the container.
- `noble-chiseled` swaps the base from `mcr.microsoft.com/dotnet/aspnet:10.0`
  (≈210 MB) to `mcr.microsoft.com/dotnet/nightly/aspnet:10.0-noble-chiseled`
  (≈95 MB). No shell, no apt, no debugging tooling — just the .NET
  runtime. Smaller pull + smaller CVE surface.

Verify post-change: API still mints the auth pipeline correctly,
DbMigrator can still write to a fresh Postgres + log to stdout.

### `README.md` at `deploy/docker/`

Five-minute deploy guide:

1. Prereqs: docker engine 24+ with compose plugin, 2 GB RAM, ports
   8080–8082 free on the host.
2. `cp .env.example .env && $EDITOR .env`. The required-to-set
   variables are flagged with `:?` in the compose file — compose will
   refuse to start until they're set.
3. `docker compose up -d --build` (first run downloads bases + builds
   four images, ~5 min).
4. Tail the migrator: `docker compose logs -f migrator`. Wait for
   "DbMigrator completed".
5. Point your external proxy at the three published ports per
   `FSH_*_URL` → host:port mapping.
6. Sign in at `https://admin.example.com` as `admin@root.com` with
   your `SEED_ADMIN_PASSWORD`. Rotate the password from `Settings →
   Security` immediately.

Plus a "swapping in managed services" appendix: comment out the
`postgres` / `redis` / `minio` services, point the connection-string /
endpoint env vars at the managed endpoints, `docker compose up -d`.

## Data flow

1. `docker compose up -d --build` parses env, fails fast if required
   secrets are missing.
2. `postgres`, `redis`, `minio` start. Health checks gate the next step.
3. `migrator` builds + runs `dotnet FSH.Starter.DbMigrator.dll apply`.
   On success, exits 0. On failure (already covered by the existing
   migrator: empty connection-string → clear error, etc.) it exits 1
   and the rest of the stack does not start.
4. `api`, `admin`, `dashboard` build + start, all gated on
   `migrator: service_completed_successfully`.
5. Frontend containers run `docker-entrypoint.sh` which renders
   `/config.json` from `${FSH_API_URL}` and then execs nginx.
6. Apps boot, fetch `/config.json`, mount React, hit the API at the
   configured URL via the operator's external proxy.

## Error handling / failure modes

| Failure | Surfaced as | Recovery |
|---|---|---|
| Missing required env var | Compose refuses to start, names the var | Set in `.env`, retry |
| Migrator can't reach Postgres | `migrator` exits 1, API never starts | Already handled — `WaitForDatabaseAsync` 12 retries × 10s |
| `Seed:DefaultAdminPassword` empty | Migrator throws at seed step | Already implemented in `IdentityDbInitializer.ResolveInitialAdminPassword` |
| `JWT_SIGNING_KEY` empty or placeholder | Host throws at `ValidateOnStart` | Already implemented in `JwtOptions.Validate` |
| Migrator succeeded once, schema drift on update | `git pull && docker compose up -d --build`; migrator re-runs idempotently | Built-in |
| Frontend container missing `FSH_API_URL` | Entrypoint exits with clear message before nginx starts | Implemented in entrypoint via `: "${FSH_API_URL:?...}"` |

## Testing

Manual + scripted smoke for v1:

1. `cp .env.example .env`, fill secrets, `docker compose up -d --build`.
   Verify all services come up healthy and migrator exits 0.
2. `curl http://localhost:8080/health` → 200.
3. `curl http://localhost:8081/` → admin SPA HTML; `curl
   http://localhost:8081/config.json` → contains the configured
   `FSH_API_URL`.
4. Hit admin via browser pointed at the host:port directly; sign in
   with the seeded admin; create a tenant; confirm webhook +
   notifications work.
5. `docker compose down && docker compose up -d` (no `--build`) — data
   in `pg_data`, `redis_data`, `minio_data` volumes survives.
6. `docker compose down -v` to wipe; re-up; confirm the stack
   re-seeds cleanly.

The publish-workflow PR that follows this one will add a CI matrix job
that runs steps 1–3 against the built compose (no browser).

## Implementation order

1. `.dockerignore` at repo root — gets the build contexts under
   control before any Dockerfile lands.
2. Frontend `main.tsx` refactor: fetch `/config.json` → window-shim →
   mount React. Same change applied to admin and dashboard. `public/config.json`
   added with dev defaults so `npm run dev` keeps working.
3. Frontend Dockerfiles + nginx + entrypoint + template (admin first,
   then dashboard — same shape, copy across). Smoke each via
   `docker build` + `docker run -p 8081:80 -e FSH_API_URL=http://localhost:5000`.
4. .NET Dockerfiles for API + DbMigrator (multi-stage, SDK build →
   aspnet-chiseled runtime).
5. Backend csproj hardening (`<ContainerUser>` + `<ContainerFamily>`).
   Smoke-test API + Migrator still boot.
6. `deploy/docker/.env.example`.
7. `deploy/docker/postgres-init/01-create-databases.sql`.
8. `deploy/docker/docker-compose.yml`.
9. `deploy/docker/README.md`.
10. End-to-end smoke on a clean machine (or fresh-volume `compose down
    -v && up -d --build`). Iterate.
11. Update root `README.md` to point at `deploy/docker/README.md` as
    the canonical deploy path.

## Out of scope (explicit deferrals)

- Helm chart / k8s manifests — v1.1.
- Multi-arch images — separate PR on the publish workflow.
- `azd up` for Azure Container Apps — v1.1.
- Built-in reverse proxy / TLS — operator owns the edge.
- "Localhost demo" mode for this compose — Aspire is the local dev path.
- Per-environment compose overlays (`docker-compose.staging.yml` etc.)
  — operators can layer with `-f` themselves; we don't ship variants.
