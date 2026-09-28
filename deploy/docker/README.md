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
| `rustfs` | `rustfs/rustfs:1.0.0` | (internal) | S3-compatible blob store for the Files module ([RustFS](https://rustfs.com)) |
| `mailpit` | `axllent/mailpit:v1.31.3` | `127.0.0.1:FSH_MAILPIT_PORT` (default 8025), SMTP 1025 internal | Local mail catcher: every e-mail the API sends lands here ([Mailpit](https://mailpit.axllent.org)) |

The compose file does **not** include a reverse proxy or TLS terminator. You bring your own edge — Cloudflare Tunnel, AWS ALB, Tailscale Funnel, your existing nginx, anything that can route a TLS subdomain to a host:port on this machine.

## Prerequisites

- Docker Engine 24+ with the Compose plugin (`docker compose version` should print v2.x).
- 2 GB free RAM, 5 GB disk for first-run images + builds.
- Ports 8080–8082 and 8025 free on the host (or set custom ports in `.env`).

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

## Reading e-mail

The API sends every e-mail (confirmation, password reset, welcome) to the bundled `mailpit` service, so nothing leaves the host and no SMTP account is needed. Open **http://localhost:8025** on the Docker host to read them and follow the links. A user an operator registers must confirm the e-mail before signing in, and the confirmation link is in that inbox.

The inbox UI is published on the host loopback only, because it holds live password-reset and confirmation links. From another machine, use an SSH tunnel (`ssh -L 8025:127.0.0.1:8025 <host>`).

To deliver real mail, set `MailOptions__Smtp__Host`, `MailOptions__Smtp__Port`, `MailOptions__Smtp__UserName`, `MailOptions__Smtp__Password` and `MailOptions__Smtp__Security` on the `api` service to your provider's values (`StartTls` for port 587, `SslOnConnect` for 465), set `FSH_MAIL_FROM` in `.env` to a sender your provider accepts, and remove the `mailpit` service and its `depends_on` entry.

## Wire up your external proxy

Point three TLS subdomains at the published ports:

| Public URL (your domain) | Host port |
|---|---|
| `api.example.com` | `8080` |
| `admin.example.com` | `8081` |
| `app.example.com` | `8082` |

Make sure the URLs you serve match the `FSH_API_URL` / `FSH_ADMIN_URL` / `FSH_DASHBOARD_URL` you set in `.env` — those values are baked into the frontends' runtime `/config.json` (CORS will fail loudly otherwise). They also drive the origins the API is allowed to put inside password-reset and e-mail-confirmation links, with `FSH_DASHBOARD_URL` as the default target for links the API sends on an operator's behalf.

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
# Repeat for fsh_redis_data and fsh_rustfs_data.
```

## Swapping in managed services

Single-host compose is the default story; production deployments often point at managed Postgres / Redis / S3. To do that:

1. Comment out the `postgres` / `redis` / `rustfs` service blocks (and `rustfs-init`) AND remove them from the `depends_on:` of `api` and `migrator`.
2. Swap the matching env vars on `api` and `migrator`:
   - `DatabaseOptions__ConnectionString` → your managed Postgres connection string (keep `GSS Encryption Mode=Disable` unless your server uses Kerberos: the chiseled images have no `libgssapi_krb5`)
   - `CachingOptions__Redis` → your managed Redis connection string (`host:port,password=...,ssl=True` etc.)
   - `Storage__Provider`, `Storage__S3__*` → your S3-compatible store
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
| No e-mail arrives, or the API logs `The SMTP server does not support the STARTTLS extension` | `MailOptions__Smtp__Security` does not match the server. The bundled Mailpit needs `None`; a provider on port 587 needs `StartTls`, on 465 `SslOnConnect`. |
| `Cannot load library libgssapi_krb5.so.2` in the API or migrator log | A connection string without `GSS Encryption Mode=Disable`. Npgsql tries GSS encryption by default and the chiseled images do not ship the Kerberos library. Harmless, but append the setting to silence it. |
| `migrator` retries Postgres for 2 minutes then fails | Postgres didn't come up — check `docker compose logs postgres`. Most often a `POSTGRES_PASSWORD` change against an existing `pg_data` volume; delete the volume with `docker compose down -v` (destructive) and start over. |
