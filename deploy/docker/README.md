# Deploy fullstackhero with Docker Compose

This brings up the full stack on a single host:

| Service | Image | Host port | What it is |
|---|---|---|---|
| `api` | `fsh/api:local` (built locally) | `FSH_API_PORT` (default 8080) | ASP.NET Core API |
| `admin` | `fsh/admin:local` | `FSH_ADMIN_PORT` (default 8081) | Operator console (nginx + React) |
| `dashboard` | `fsh/dashboard:local` | `FSH_DASHBOARD_PORT` (default 8082) | Tenant dashboard (nginx + React) |
| `migrator` | `fsh/dbmigrator:local` | — | One-shot: applies EF migrations + seeds the root tenant + creates the default admin user |
| `postgres` | `postgres:18-alpine` | (internal) | Identity, tenant catalog, module schemas |
| `redis` | `valkey/valkey:9.1.0-alpine` | (internal) | HybridCache L2, Data Protection keys, idempotency store |
| `rustfs` | `rustfs/rustfs:1.0.0` | `FSH_S3_PORT` (default 9000), S3 API only | S3-compatible blob store for the Files module ([RustFS](https://rustfs.com)); published because browsers upload to it through presigned URLs |

The compose file does **not** include a reverse proxy or TLS terminator. You bring your own edge — Cloudflare Tunnel, AWS ALB, Tailscale Funnel, your existing nginx, anything that can route a TLS subdomain to a host:port on this machine.

## Prerequisites

- Docker Engine 24+ with the Compose plugin (`docker compose version` should print v2.x).
- 2 GB free RAM, 5 GB disk for first-run images + builds.
- Ports 8080–8082 and 9000 free on the host (or set custom ports in `.env`).

## Five-minute deploy

```bash
cp .env.example .env
$EDITOR .env             # fill JWT_SIGNING_KEY, SEED_ADMIN_PASSWORD, the data-plane passwords, and your four URLs

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
curl -fsS  http://localhost:9000/health       # RustFS S3 API
```

## Wire up your external proxy

Point four TLS subdomains at the published ports:

| Public URL (your domain) | Host port |
|---|---|
| `api.example.com` | `8080` |
| `admin.example.com` | `8081` |
| `app.example.com` | `8082` |
| `s3.example.com` | `9000` |

Make sure the URLs you serve match the `FSH_API_URL` / `FSH_ADMIN_URL` / `FSH_DASHBOARD_URL` you set in `.env` — those values are baked into the frontends' runtime `/config.json` (CORS will fail loudly otherwise). They also drive the origins the API is allowed to put inside password-reset and e-mail-confirmation links, with `FSH_DASHBOARD_URL` as the default target for links the API sends on an operator's behalf.

Presigned uploads and downloads skip the API: the browser talks to RustFS through presigned URLs, which the API signs for `FSH_S3_PUBLIC_URL` (it reaches RustFS itself on the internal `http://rustfs:9000`). Set `FSH_S3_PUBLIC_URL` to the URL your proxy serves the S3 port on, and make the proxy forward the original `Host` header: the signature covers it, so a rewritten host fails with `SignatureDoesNotMatch`. RustFS only grants CORS to `FSH_ADMIN_URL` and `FSH_DASHBOARD_URL`, so a browser on any other origin blocks the PUT.

Public files (avatars, product images) are served without a signature: the API keeps them under the `public/` prefix, hands out `FSH_S3_PUBLIC_URL/fsh/public/...` URLs, and `rustfs-init` grants anonymous read on `public/*` of the `fsh` bucket only. Private files live under `private/` and are only reachable through presigned URLs.

**Lock down the S3 port.** Compose publishes `FSH_S3_PORT` on all interfaces. Firewall it so only your proxy can reach it, or, when the proxy runs on the same host, bind it to loopback by changing the `rustfs` port mapping to `"127.0.0.1:${FSH_S3_PORT:-9000}:9000"`.

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

Upgrading from a release where public files had no `public/` prefix: `rustfs-init` re-applies the bucket policy on every `up`, and on start the API moves those files under `public/` (and rewrites the avatar and product-image URLs that point at them) in a background job. Watch `docker compose logs api` for `[Files] moved … legacy public file(s)`; failures are logged, retried by the job scheduler with backoff, and retried again on every API start. Uploads that were presigned before the upgrade are moved when they finalize.

## Backing up

The three named volumes hold all state:

```bash
docker run --rm \
  -v fsh_pg_data:/source:ro \
  -v "$PWD":/backup \
  alpine \
  tar czf /backup/pg_data-$(date +%Y%m%d).tar.gz -C /source .
# Repeat for fsh_redis_data and fsh_rustfs_data.
```

## Swapping in managed services

Single-host compose is the default story; production deployments often point at managed Postgres / Redis / S3. To do that:

1. Comment out the `postgres` / `redis` / `rustfs` service blocks (and `rustfs-init`) AND remove them from the `depends_on:` of `api` and `migrator`.
2. Swap the matching env vars on `api` and `migrator`:
   - `DatabaseOptions__ConnectionString` → your managed Postgres connection string
   - `CachingOptions__Redis` → your managed Redis connection string (`host:port,password=...,ssl=True` etc.)
   - `Storage__Provider`, `Storage__S3__*` → your S3-compatible store (drop `Storage__S3__PresignServiceUrl` when the store's own endpoint is reachable from browsers, as with AWS S3)
3. `docker compose up -d`.

The data-plane volumes (`pg_data`, `redis_data`, `rustfs_data`) can be deleted once you've migrated.

## Troubleshooting

| Symptom | Likely cause |
|---|---|
| `xxx_PASSWORD is required` at `docker compose up` | A required env var is empty in `.env`. The error names the var. |
| Migrator exits non-zero with `Failed to fetch dynamically imported module` | A frontend bundle baked the wrong API URL. Check `FSH_API_URL` in `.env` and re-run with `--build`. |
| `OptionsValidationException: SigningKey looks like a sample placeholder` | `JWT_SIGNING_KEY` contains `replace-with` (the framework's placeholder detector). Generate a real key: `openssl rand -base64 48`. |
| API up but admin shows a CORS error | `FSH_ADMIN_URL` / `FSH_DASHBOARD_URL` in `.env` doesn't match what your external proxy serves. Both go on the CORS allow-list. |
| A reset or confirmation e-mail links to the API instead of the app | Same cause: `FSH_ADMIN_URL` / `FSH_DASHBOARD_URL` don't match the origins the browser actually uses. Both also feed `FrontendOptions__AllowedOrigins`, and `FSH_DASHBOARD_URL` feeds `FrontendOptions__DefaultOrigin`. |
| An avatar or product image doesn't render (`403 AccessDenied` on the image URL) | The URL's key doesn't start with `public/`, or the bucket policy is missing. Check `docker compose logs rustfs-init` (it must exit 0) and the API log for the legacy-file migration. |
| File upload fails in the browser (network error, CORS error, or `403 SignatureDoesNotMatch`) | `FSH_S3_PUBLIC_URL` doesn't match what your proxy serves on the S3 port, the proxy rewrites the `Host` header, or the page's origin isn't `FSH_ADMIN_URL` / `FSH_DASHBOARD_URL` (the RustFS CORS allow-list). |
| `migrator` retries Postgres for 2 minutes then fails | Postgres didn't come up — check `docker compose logs postgres`. Most often a `POSTGRES_PASSWORD` change against an existing `pg_data` volume; delete the volume with `docker compose down -v` (destructive) and start over. |
