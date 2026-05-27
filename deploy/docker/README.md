# Deploy fullstackhero with Docker Compose

This brings up the full stack on a single host:

| Service | Image | Host port | What it is |
|---|---|---|---|
| `api` | `fsh/api:local` (built locally) | `FSH_API_PORT` (default 8080) | ASP.NET Core API |
| `admin` | `fsh/admin:local` | `FSH_ADMIN_PORT` (default 8081) | Operator console (nginx + React) |
| `dashboard` | `fsh/dashboard:local` | `FSH_DASHBOARD_PORT` (default 8082) | Tenant dashboard (nginx + React) |
| `migrator` | `fsh/dbmigrator:local` | — | One-shot: applies EF migrations + seeds the root tenant + creates the default admin user |
| `postgres` | `postgres:17-alpine` | (internal) | Identity, tenant catalog, module schemas |
| `redis` | `redis:7-alpine` | (internal) | HybridCache L2, Data Protection keys, idempotency store |
| `minio` | `minio/minio:latest` | (internal) | S3-compatible blob store for the Files module |

The compose file does **not** include a reverse proxy or TLS terminator. You bring your own edge — Cloudflare Tunnel, AWS ALB, Tailscale Funnel, your existing nginx, anything that can route a TLS subdomain to a host:port on this machine.

## Prerequisites

- Docker Engine 24+ with the Compose plugin (`docker compose version` should print v2.x).
- 2 GB free RAM, 5 GB disk for first-run images + builds.
- Ports 8080–8082 free on the host (or set custom ports in `.env`).

## Five-minute deploy

```bash
cp .env.example .env
$EDITOR .env             # fill JWT_SIGNING_KEY, SEED_ADMIN_PASSWORD, the data-plane passwords, and your three URLs

docker compose up -d --build
```

First run downloads bases + builds four images (~5 min). Subsequent runs are cached.

```bash
docker compose logs -f migrator
```

Wait until you see something like `[migrator] DbMigrator completed` and the `migrator` container exits 0. `api`, `admin`, `dashboard` start automatically after.

## Verify it's healthy

```bash
curl -fsS http://localhost:8080/health/live   # API liveness
curl -fsSI http://localhost:8081/ | head -1   # admin SPA — HTTP/1.1 200 OK
curl -fsS  http://localhost:8081/config.json  # admin runtime config — shows FSH_API_URL
curl -fsSI http://localhost:8082/ | head -1   # dashboard SPA
```

## Wire up your external proxy

Point three TLS subdomains at the published ports:

| Public URL (your domain) | Host port |
|---|---|
| `api.example.com` | `8080` |
| `admin.example.com` | `8081` |
| `app.example.com` | `8082` |

Make sure the URLs you serve match the `FSH_API_URL` / `FSH_ADMIN_URL` / `FSH_DASHBOARD_URL` you set in `.env` — those values are baked into the frontends' runtime `/config.json` (CORS will fail loudly otherwise).

## Sign in for the first time

Open `https://admin.example.com`, sign in as:

- **email:** `admin@root.com`
- **tenant:** `root`
- **password:** whatever you set as `SEED_ADMIN_PASSWORD`

Rotate the password from **Settings → Security** immediately.

## Updating

```bash
git pull
docker compose up -d --build
```

The `migrator` re-runs and applies any new migrations idempotently before `api` restarts.

## Backing up

The three named volumes hold all state:

```bash
docker run --rm \
  -v fsh_pg_data:/source:ro \
  -v "$PWD":/backup \
  alpine \
  tar czf /backup/pg_data-$(date +%Y%m%d).tar.gz -C /source .
# Repeat for fsh_redis_data and fsh_minio_data.
```

## Swapping in managed services

Single-host compose is the default story; production deployments often point at managed Postgres / Redis / S3. To do that:

1. Comment out the `postgres` / `redis` / `minio` service blocks AND remove them from the `depends_on:` of `api` and `migrator`.
2. Swap the matching env vars on `api` and `migrator`:
   - `DatabaseOptions__ConnectionString` → your managed Postgres connection string
   - `CachingOptions__Redis` → your managed Redis connection string (`host:port,password=...,ssl=True` etc.)
   - `Storage__Provider`, `Storage__S3__*` → your S3-compatible store
3. `docker compose up -d`.

The data-plane volumes (`pg_data`, `redis_data`, `minio_data`) can be deleted once you've migrated.

## Troubleshooting

| Symptom | Likely cause |
|---|---|
| `xxx_PASSWORD is required` at `docker compose up` | A required env var is empty in `.env`. The error names the var. |
| Migrator exits non-zero with `Failed to fetch dynamically imported module` | A frontend bundle baked the wrong API URL. Check `FSH_API_URL` in `.env` and re-run with `--build`. |
| `OptionsValidationException: SigningKey looks like a sample placeholder` | `JWT_SIGNING_KEY` contains `replace-with` (the framework's placeholder detector). Generate a real key: `openssl rand -base64 48`. |
| API up but admin shows a CORS error | `FSH_ADMIN_URL` / `FSH_DASHBOARD_URL` in `.env` doesn't match what your external proxy serves. Both go on the CORS allow-list. |
| `migrator` retries Postgres for 2 minutes then fails | Postgres didn't come up — check `docker compose logs postgres`. Most often a `POSTGRES_PASSWORD` change against an existing `pg_data` volume; delete the volume with `docker compose down -v` (destructive) and start over. |
