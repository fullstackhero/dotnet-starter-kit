# 2026-07-02 — First production deployment to Hostinger VPS

**Server:** `srv1784663.hstgr.cloud` / `2.25.69.231` — Hostinger KVM 2 (2 vCPU, 8 GB RAM, 100 GB NVMe), Ubuntu 24.04
**Domain:** `sabinstack.cloud` → `api.` / `admin.` / `app.` subdomains
**Outcome:** full stack live over HTTPS; DB tooling (pgAdmin, DBeaver) wired up over SSH tunnels; repo fixes on PR [#1](https://github.com/sabinshrestha/dotnet-starter-kit/pull/1)

---

## What was done (chronological)

1. **OS prep** — `apt update && apt upgrade -y`, reboot for new kernel (6.8.0-134).
2. **Docker** — installed via `curl -fsSL https://get.docker.com | sh` (Engine 29.6.1).
3. **Clone + configure** — cloned repo, `cp .env.example .env` in `deploy/docker/`,
   generated secrets (`openssl rand -base64 48` for JWT, `openssl rand -hex 16` for the rest).
4. **First launch** — `docker compose up -d --build` (build took ~3.7 min).
   ❌ `fsh-postgres` crashed — see Error 1 below. Fixed, relaunched, migrator seeded successfully.
5. **DNS** — three A records (`api`, `admin`, `app`) → `2.25.69.231` in Hostinger DNS
   (domain also carries Hostinger mail/parking records — untouched, they don't conflict).
6. **Caddy** — installed from the official apt repo; Caddyfile with the three subdomains
   reverse-proxying to 8080/8081/8082.
   ❌ First attempt used the literal placeholder `yourdomain.com` — see Error 2. Fixed;
   Let's Encrypt certificates issued for all three names at 00:40 UTC.
7. **Production URLs** — `.env` switched to `https://` URLs; app ports rebound to
   loopback (`FSH_API_PORT=127.0.0.1:8080` etc.); `docker compose up -d --force-recreate api admin dashboard`.
8. **Firewall** — `ufw allow 22,80,443/tcp && ufw enable`. Hostinger cloud firewall left at 0 rules.
   ❌ Site unreachable from one network — see Error 3 (not actually a server problem).
9. **DB tooling:**
   - Removed a Hostinger-catalog Adminer (publicly exposed on `:32768`, wrong Docker
     network — couldn't resolve `fsh-postgres`) and a catalog pgAdmin (same problems).
   - pgAdmin installed properly as its own compose project (`~/pgadmin/docker-compose.yml`):
     image `dpage/pgadmin4`, bound `127.0.0.1:8084:80`, joined to external network `fsh_default`.
   - Postgres given a loopback-only host port: `127.0.0.1:5432:5432` in `deploy/docker/docker-compose.yml`.
   - DBeaver on the PC connects via its built-in SSH tunnel (key auth) → `localhost:5432`.
10. **Repo changes** (branch `claude/hostinger-vps-hosting-kfl6zu`, PR #1):
    postgres 18 volume fix, `deploy/docker/DEPLOY-VPS.md` runbook with mermaid diagrams,
    this worklog.

## Errors hit & fixes

### Error 1 — `fsh-postgres` unhealthy, dies ~1 s after start

```
✘ Container fsh-postgres  Error dependency postgres failed to start
Error: in 18+, these Docker images are configured to store database data in ...
```

**Cause:** `postgres:18` images moved PGDATA to `/var/lib/postgresql/18/docker` and refuse
a volume mounted at the legacy `/var/lib/postgresql/data` path.
**Fix:** mount `pg_data:/var/lib/postgresql` instead; `docker compose down -v` (safe —
first boot, empty DB) and relaunch. Committed to the repo so fresh clones don't hit it.

### Error 2 — Caddy certificate failures for `yourdomain.com`

```
"error":"HTTP 400 urn:ietf:params:acme:error:tls - ... tls: no application protocol"
```

**Cause:** placeholder `yourdomain.com` pasted into `/etc/caddy/Caddyfile` unchanged.
**Fix:** real hostnames (`*.sabinstack.cloud`), `systemctl reload caddy` — certs issued in seconds.

### Error 3 — `ERR_CONNECTION_TIMED_OUT` from the office network only

Server-side curls returned 200; phone on mobile data worked; one network timed out.
**Cause:** corporate web filter blocking a **newly-registered domain** — nothing
server-side. Diagnosed by elimination + (optionally) `tcpdump -ni any 'tcp port 443'`
showing no inbound SYNs. **Fix:** wait for domain categorization (24–72 h) or ask IT
to whitelist. Documented in the runbook so future deploys don't chase ghosts.

### Gotchas confirmed along the way

- **Docker-published ports bypass ufw** → anything that must be private is bound to
  `127.0.0.1`, never relying on the firewall.
- **Hostinger catalog apps** publish on random public ports on their own Docker
  network → don't use the catalog for DB tools; self-install with loopback + `fsh_default`.
- **Hostinger Docker Manager** only lists *compose projects* — plain `docker run`
  containers are invisible there (run tools as mini compose projects instead).
- SSH `Permission denied` while already on the VPS = tunnel command typed in the wrong
  window; tunnels always run **on the PC**. Key passphrase ≠ server password.

## How to look up credentials (no secrets stored here!)

All secrets live in **`~/dotnet-starter-kit/deploy/docker/.env` on the VPS** (never in git):

```bash
grep POSTGRES_PASSWORD ~/dotnet-starter-kit/deploy/docker/.env   # DB password (user fsh, db fsh)
grep SEED_ADMIN_PASSWORD ~/dotnet-starter-kit/deploy/docker/.env # app admin (admin@root.com, tenant root)
grep -E 'HANGFIRE|MINIO|REDIS|JWT' ~/dotnet-starter-kit/deploy/docker/.env
```

pgAdmin's own login is set in `~/pgadmin/docker-compose.yml` (`PGADMIN_DEFAULT_*`).
App admin password: rotated in-app after first login — the `.env` seed value is bootstrap-only.

## Daily-use commands

```bash
# DB browser (pgAdmin) — run on the PC, keep window open, browse http://localhost:8084
ssh -L 8084:localhost:8084 root@2.25.69.231

# DBeaver: SSH tab → 2.25.69.231/root/key ; Main tab → localhost:5432, db fsh, user fsh

# Update the app to latest main
cd ~/dotnet-starter-kit && git pull && cd deploy/docker && docker compose up -d --build

# Logs / status
docker compose -f ~/dotnet-starter-kit/deploy/docker/docker-compose.yml ps
docker compose -f ~/dotnet-starter-kit/deploy/docker/docker-compose.yml logs -f api
```

## Risk register (state at end of session)

| # | Risk | Severity | Status / mitigation |
|---|---|---|---|
| 1 | SSH password auth still enabled (`passwordauthentication yes`) — internet bots brute-force root continuously | **High** | **OPEN — next action.** Key login verified working; set `PasswordAuthentication no` + `PermitRootLogin prohibit-password`, keep hPanel browser terminal as recovery door |
| 2 | No backups configured (Hostinger snapshot count: 0; no volume backups) | **High** | **OPEN.** Enable Hostinger auto-backup ($6/mo) and/or cron the volume `tar` loop from the runbook |
| 3 | Secrets exist only in `.env` on the VPS — server loss = secrets loss | Medium | **OPEN.** Keep an offline copy in a password manager |
| 4 | `localhost:5432` now reachable by any process on the VPS (loopback publish for DBeaver) | Low | Accepted — strong 32-char password; keep untrusted software off the box |
| 5 | Direct prod-DB editing via pgAdmin/DBeaver can corrupt app-managed state | Low | Set DBeaver connection type to *Production*; prefer the admin app for writes |
| 6 | PR #1 unmerged — fresh clones of `main` still hit the postgres 18 crash | Medium | Merge PR #1 |
| 7 | Root admin seed password was in `.env` and typed around | Low | Rotated in-app (Settings → Security) — verify done |

## Current state snapshot

- **Public surface:** ports 22, 80, 443 only. TLS by Caddy (auto-renew).
- **Compose projects:** `fsh` (8 containers), `pgadmin` (1). Adminer & catalog apps removed.
- **Idle RAM:** ~437 MB of 8 GB. Disk: ~9 % of 96 GB.
- **URLs:** https://admin.sabinstack.cloud · https://app.sabinstack.cloud · https://api.sabinstack.cloud (`/scalar`, `/jobs`)
