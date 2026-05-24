# Production Docker Deploy — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship the v1 "deploy this starter kit" story — frontend Dockerfiles, a single-host production `docker-compose.yml`, and `.env.example` at `deploy/docker/`. Bundle the API + DbMigrator container hardening (drop root, chiseled runtime) while we're touching infra.

**Architecture:** Single-host compose with everything inside (Postgres, Redis, MinIO + the FSH services). Subdomain-style routing — operator terminates TLS externally; compose only publishes raw ports. Frontends use runtime `/config.json` (rendered from env by an entrypoint shim at container start), so one image works for any deploy.

**Tech Stack:** docker compose v2, multi-stage Dockerfiles, nginx:alpine for static serving + envsubst, mcr.microsoft.com/dotnet/sdk:10.0 for build / `nightly/aspnet:10.0-noble-chiseled` for runtime.

**Source spec:** [`docs/superpowers/specs/2026-05-19-production-docker-deploy-design.md`](../specs/2026-05-19-production-docker-deploy-design.md)

---

## Files at a glance

| Path | New / Modify | Owner |
|---|---|---|
| `.dockerignore` (repo root) | Modify | Task 1 |
| `clients/admin/src/env.ts` | Modify | Task 2 |
| `clients/admin/src/main.tsx` | Modify | Task 2 |
| `clients/admin/public/config.json` | Create | Task 2 |
| `clients/dashboard/src/env.ts` | Modify | Task 3 |
| `clients/dashboard/src/main.tsx` | Modify | Task 3 |
| `clients/dashboard/public/config.json` | Create | Task 3 |
| `clients/admin/Dockerfile` | Create | Task 4 |
| `clients/admin/docker/nginx.conf` | Create | Task 4 |
| `clients/admin/docker/config.json.template` | Create | Task 4 |
| `clients/admin/docker/docker-entrypoint.sh` | Create | Task 4 |
| `clients/admin/.dockerignore` | Create | Task 4 |
| `clients/dashboard/Dockerfile` | Create | Task 5 |
| `clients/dashboard/docker/nginx.conf` | Create | Task 5 |
| `clients/dashboard/docker/config.json.template` | Create | Task 5 |
| `clients/dashboard/docker/docker-entrypoint.sh` | Create | Task 5 |
| `clients/dashboard/.dockerignore` | Create | Task 5 |
| `src/Host/FSH.Starter.Api/Dockerfile` | Modify | Task 6 |
| `src/Host/FSH.Starter.Api/FSH.Starter.Api.csproj` | Modify | Task 6 |
| `src/Host/FSH.Starter.DbMigrator/Dockerfile` | Create | Task 7 |
| `src/Host/FSH.Starter.DbMigrator/FSH.Starter.DbMigrator.csproj` | Modify | Task 7 |
| `deploy/docker/.env.example` | Create | Task 8 |
| `deploy/docker/postgres-init/01-create-databases.sql` | Create | Task 9 |
| `deploy/docker/docker-compose.yml` | Create | Task 10 |
| `deploy/docker/README.md` | Create | Task 11 |
| `README.md` (repo root) | Modify | Task 13 |

---

### Task 1: Tighten the repo-root `.dockerignore`

**Files:**
- Modify: `.dockerignore`

The current file excludes `bin/`, `obj/`, `.vs/`, `node_modules/`, `.git`, `*.md`, `src/Tests/`. We need to add the frontend build outputs and test artifacts so the docker build context for every image stays small.

- [ ] **Step 1: Overwrite the .dockerignore with the extended set**

Replace the file contents with:

```
# .NET build artifacts
**/bin
**/obj
**/.vs
**/TestResults

# Node / frontend build artifacts
**/node_modules
**/dist
**/.vite
**/coverage
**/playwright-report
**/test-results

# Test projects (not needed in any runtime image)
src/Tests/

# VCS / editor / docs / local dev
.git
.github
.idea
.vscode
*.md
LICENSE
**/.env
**/.env.*
!**/.env.example
deploy/
docs/
clients/dashboard/public/config.json
clients/admin/public/config.json
```

Notes:
- The two `public/config.json` excludes prevent dev defaults from leaking into the production image; the entrypoint will create the runtime file from the template.
- `deploy/` is excluded because the compose file's `context: ../..` would otherwise drag the compose + env files into every image's build context.
- `!**/.env.example` un-ignores example files in case any project ever ships one in source.

- [ ] **Step 2: Commit**

```bash
git add .dockerignore
git commit -m "build(docker): tighten root .dockerignore for frontend + test artifacts"
```

---

### Task 2: Admin app — runtime `/config.json` boot

**Files:**
- Modify: `clients/admin/src/env.ts`
- Modify: `clients/admin/src/main.tsx`
- Create: `clients/admin/public/config.json`

The admin currently reads `VITE_API_BASE_URL`, `VITE_DASHBOARD_URL`, `VITE_DEFAULT_TENANT` at module-load time from build-baked Vite env. We're switching to a runtime fetch so one image works for any deploy. Same surface (`env.apiBase`, `env.dashboardUrl`, `env.defaultTenant`); different source.

- [ ] **Step 1: Replace `src/env.ts` with a runtime-config implementation**

Overwrite `clients/admin/src/env.ts` with:

```ts
// Runtime config — fetched once at boot from /config.json (served by the
// frontend's own nginx in production, by Vite from public/config.json in
// dev). One image works for every deploy; the operator wires API_URL /
// DASHBOARD_URL into the JSON file via envsubst at container start.
type RuntimeConfig = {
  apiBase: string;
  defaultTenant: string;
  dashboardUrl: string;
};

let cached: RuntimeConfig | null = null;

export async function loadRuntimeConfig(): Promise<void> {
  if (cached !== null) return;
  const res = await fetch("/config.json", { cache: "no-store" });
  if (!res.ok) {
    throw new Error(`Failed to load /config.json: ${res.status} ${res.statusText}`);
  }
  const cfg = (await res.json()) as Partial<RuntimeConfig>;
  cached = {
    apiBase: (cfg.apiBase ?? "").replace(/\/$/, ""),
    defaultTenant: cfg.defaultTenant ?? "root",
    // Dashboard origin used by the impersonation handoff. Dev default
    // mirrors clients/dashboard/package.json's vite port.
    dashboardUrl: (cfg.dashboardUrl ?? "http://localhost:5174").replace(/\/$/, ""),
  };
}

function get(): RuntimeConfig {
  if (cached === null) {
    throw new Error(
      "Runtime config not loaded. main.tsx must await loadRuntimeConfig() before mounting React.",
    );
  }
  return cached;
}

export const env = {
  get apiBase(): string { return get().apiBase; },
  get defaultTenant(): string { return get().defaultTenant; },
  get dashboardUrl(): string { return get().dashboardUrl; },
};
```

- [ ] **Step 2: Replace `src/main.tsx` to await config before mounting**

Overwrite `clients/admin/src/main.tsx` with:

```tsx
import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { App } from "@/App";
import { loadRuntimeConfig } from "@/env";
import "@/styles/globals.css";

// Runtime config must be in-memory BEFORE any module that reads env.*
// runs in a render or hook. Top-level await is supported in Vite's ESM
// output and is the simplest shape that guarantees ordering.
await loadRuntimeConfig();

const rootElement = document.getElementById("root");
if (!rootElement) {
  throw new Error("Root element '#root' not found");
}

createRoot(rootElement).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
```

- [ ] **Step 3: Add dev defaults at `public/config.json`**

Create `clients/admin/public/config.json` with:

```json
{
  "apiBase": "",
  "defaultTenant": "root",
  "dashboardUrl": "http://localhost:5174"
}
```

(Empty `apiBase` keeps the existing dev behaviour where the Vite proxy handles `/api` → backend.)

- [ ] **Step 4: Verify dev build still works**

```bash
cd clients/admin && npx tsc --noEmit
```
Expected: no errors.

```bash
npm run build
```
Expected: succeeds, emits `dist/`.

- [ ] **Step 5: Run admin Playwright suite to confirm no regression**

```bash
cd clients/admin && npx playwright test --workers 1 --reporter=line
```
Expected: `18 passed`. (Tests serve via Vite dev which reads `public/config.json`.)

- [ ] **Step 6: Commit**

```bash
git add clients/admin/src/env.ts clients/admin/src/main.tsx clients/admin/public/config.json
git commit -m "refactor(admin): load /config.json at boot for runtime-configurable API URL"
```

---

### Task 3: Dashboard app — runtime `/config.json` boot

**Files:**
- Modify: `clients/dashboard/src/env.ts`
- Modify: `clients/dashboard/src/main.tsx`
- Create: `clients/dashboard/public/config.json`

Same shape as Task 2 minus the admin-only `dashboardUrl`.

- [ ] **Step 1: Replace `src/env.ts`**

Overwrite `clients/dashboard/src/env.ts` with:

```ts
// Runtime config — fetched once at boot from /config.json. See
// clients/admin/src/env.ts for the rationale; the dashboard doesn't
// need dashboardUrl (the handoff is one-way: admin → dashboard).
type RuntimeConfig = {
  apiBase: string;
  defaultTenant: string;
};

let cached: RuntimeConfig | null = null;

export async function loadRuntimeConfig(): Promise<void> {
  if (cached !== null) return;
  const res = await fetch("/config.json", { cache: "no-store" });
  if (!res.ok) {
    throw new Error(`Failed to load /config.json: ${res.status} ${res.statusText}`);
  }
  const cfg = (await res.json()) as Partial<RuntimeConfig>;
  cached = {
    apiBase: (cfg.apiBase ?? "").replace(/\/$/, ""),
    defaultTenant: cfg.defaultTenant ?? "root",
  };
}

function get(): RuntimeConfig {
  if (cached === null) {
    throw new Error(
      "Runtime config not loaded. main.tsx must await loadRuntimeConfig() before mounting React.",
    );
  }
  return cached;
}

export const env = {
  get apiBase(): string { return get().apiBase; },
  get defaultTenant(): string { return get().defaultTenant; },
};
```

- [ ] **Step 2: Replace `src/main.tsx`**

Overwrite `clients/dashboard/src/main.tsx` with:

```tsx
import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { App } from "@/App";
import { installImpersonationFromHash } from "@/auth/impersonation-handoff";
import { loadRuntimeConfig } from "@/env";
import "@/styles/globals.css";

// Runtime config must resolve before React mounts so env.apiBase reads
// inside components see the right value on first paint.
await loadRuntimeConfig();

const rootElement = document.getElementById("root");
if (!rootElement) {
  throw new Error("Root element '#root' not found");
}

// Cross-app impersonation handoff — must run BEFORE createRoot so the
// installed token is visible to AuthProvider on first paint. See the
// helper docstring for the why.
installImpersonationFromHash();

createRoot(rootElement).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
```

- [ ] **Step 3: Add dev defaults at `public/config.json`**

Create `clients/dashboard/public/config.json` with:

```json
{
  "apiBase": "",
  "defaultTenant": "root"
}
```

- [ ] **Step 4: Verify dev build**

```bash
cd clients/dashboard && npx tsc --noEmit && npm run build
```
Expected: no errors; `dist/` emitted.

- [ ] **Step 5: Run dashboard Playwright suite**

```bash
cd clients/dashboard && npx playwright test --workers 1 --reporter=line
```
Expected: `37 passed`.

- [ ] **Step 6: Commit**

```bash
git add clients/dashboard/src/env.ts clients/dashboard/src/main.tsx clients/dashboard/public/config.json
git commit -m "refactor(dashboard): load /config.json at boot for runtime-configurable API URL"
```

---

### Task 4: Admin frontend Dockerfile + nginx + entrypoint

**Files:**
- Create: `clients/admin/Dockerfile`
- Create: `clients/admin/docker/nginx.conf`
- Create: `clients/admin/docker/config.json.template`
- Create: `clients/admin/docker/docker-entrypoint.sh`
- Create: `clients/admin/.dockerignore`

- [ ] **Step 1: Per-package `.dockerignore` to keep the build context tiny**

Create `clients/admin/.dockerignore`:

```
node_modules
dist
.vite
coverage
playwright-report
test-results
tests
*.log
.env
.env.*
!.env.example
public/config.json
```

(The local exclude duplicates parts of the root file but covers single-context builds where we point `docker build` at this directory directly.)

- [ ] **Step 2: Create `docker/nginx.conf`**

Create `clients/admin/docker/nginx.conf`:

```nginx
server {
  listen 80;
  server_name _;
  root /usr/share/nginx/html;
  index index.html;

  # Long-lived caching for hashed asset bundles
  location ~* \.(?:js|css|woff2?|png|jpg|jpeg|svg|ico|webp)$ {
    expires 1y;
    add_header Cache-Control "public, immutable";
  }

  # Runtime config — must be re-fetched on every deploy
  location = /config.json {
    add_header Cache-Control "no-store";
    add_header X-Content-Type-Options "nosniff";
  }

  # SPA fallback with security headers
  location / {
    try_files $uri /index.html;
    add_header X-Frame-Options "DENY";
    add_header X-Content-Type-Options "nosniff";
    add_header Referrer-Policy "strict-origin-when-cross-origin";
  }

  # Block dotfiles
  location ~ /\. {
    deny all;
  }
}
```

- [ ] **Step 3: Create `docker/config.json.template`**

Create `clients/admin/docker/config.json.template`:

```json
{
  "apiBase": "${FSH_API_URL}",
  "defaultTenant": "${FSH_DEFAULT_TENANT}",
  "dashboardUrl": "${FSH_DASHBOARD_URL}"
}
```

- [ ] **Step 4: Create the entrypoint script**

Create `clients/admin/docker/docker-entrypoint.sh`:

```bash
#!/bin/sh
set -e

# Fail fast on missing required values rather than serve a broken bundle.
: "${FSH_API_URL:?FSH_API_URL is required (e.g. https://api.example.com)}"
: "${FSH_DASHBOARD_URL:?FSH_DASHBOARD_URL is required (e.g. https://app.example.com)}"

# Defaults for non-required values.
: "${FSH_DEFAULT_TENANT:=root}"

export FSH_API_URL FSH_DASHBOARD_URL FSH_DEFAULT_TENANT

# Render the runtime config from the template, writing into nginx's web root.
envsubst < /usr/share/nginx/html/config.json.template \
       > /usr/share/nginx/html/config.json

# Drop the template so it isn't served accidentally.
rm /usr/share/nginx/html/config.json.template

exec nginx -g 'daemon off;'
```

- [ ] **Step 5: Create the multi-stage Dockerfile**

Create `clients/admin/Dockerfile`:

```dockerfile
# syntax=docker/dockerfile:1.7

# Stage 1: build the SPA bundle
FROM node:22-alpine AS build
WORKDIR /app

# Install deps with a cached layer
COPY package.json package-lock.json ./
RUN npm ci

# Build
COPY . .
RUN npm run build

# Stage 2: serve via nginx
FROM nginx:alpine AS runtime
WORKDIR /usr/share/nginx/html

# nginx:alpine needs `envsubst` from gettext (it ships busybox without it).
RUN apk add --no-cache gettext

# Replace the default nginx site
RUN rm -rf ./* /etc/nginx/conf.d/default.conf
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf

# Copy the built bundle, the runtime config template, and the entrypoint
COPY --from=build /app/dist/ ./
COPY docker/config.json.template ./config.json.template
COPY docker/docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh

# Non-root: nginx:alpine includes the `nginx` user (uid 101).
# We do NOT switch to it here because the upstream image's master process
# needs root to bind :80; nginx will fork workers as the `nginx` user on
# its own per its default config.

EXPOSE 80
ENTRYPOINT ["/docker-entrypoint.sh"]
```

- [ ] **Step 6: Build and smoke-test**

```bash
cd clients/admin
docker build -t fsh/admin:smoke .
```
Expected: build succeeds in ~60–90s on a warm cache.

```bash
docker run --rm -d --name fsh-admin-smoke \
  -p 18081:80 \
  -e FSH_API_URL=http://localhost:5000 \
  -e FSH_DASHBOARD_URL=http://localhost:5174 \
  fsh/admin:smoke
sleep 2
curl -fsS http://localhost:18081/config.json
```
Expected output:
```json
{
  "apiBase": "http://localhost:5000",
  "defaultTenant": "root",
  "dashboardUrl": "http://localhost:5174"
}
```

```bash
curl -fsSI http://localhost:18081/ | head -1
```
Expected: `HTTP/1.1 200 OK`.

```bash
docker logs fsh-admin-smoke | tail -3
docker rm -f fsh-admin-smoke
```
Expected: no errors in logs.

- [ ] **Step 7: Verify the missing-env failure mode**

```bash
docker run --rm fsh/admin:smoke 2>&1 | head -3
```
Expected: `FSH_API_URL is required (e.g. https://api.example.com)` and the container exits non-zero.

- [ ] **Step 8: Commit**

```bash
git add clients/admin/Dockerfile clients/admin/.dockerignore clients/admin/docker/
git commit -m "build(admin): Dockerfile with nginx + runtime /config.json envsubst"
```

---

### Task 5: Dashboard frontend Dockerfile + nginx + entrypoint

**Files:**
- Create: `clients/dashboard/Dockerfile`
- Create: `clients/dashboard/docker/nginx.conf`
- Create: `clients/dashboard/docker/config.json.template`
- Create: `clients/dashboard/docker/docker-entrypoint.sh`
- Create: `clients/dashboard/.dockerignore`

Same shape as Task 4 minus the `FSH_DASHBOARD_URL` plumbing (dashboard doesn't need it).

- [ ] **Step 1: Create per-package `.dockerignore`**

Create `clients/dashboard/.dockerignore` (identical to admin's):

```
node_modules
dist
.vite
coverage
playwright-report
test-results
tests
*.log
.env
.env.*
!.env.example
public/config.json
```

- [ ] **Step 2: Create `docker/nginx.conf`**

Same content as admin's nginx.conf. Create `clients/dashboard/docker/nginx.conf`:

```nginx
server {
  listen 80;
  server_name _;
  root /usr/share/nginx/html;
  index index.html;

  location ~* \.(?:js|css|woff2?|png|jpg|jpeg|svg|ico|webp)$ {
    expires 1y;
    add_header Cache-Control "public, immutable";
  }

  location = /config.json {
    add_header Cache-Control "no-store";
    add_header X-Content-Type-Options "nosniff";
  }

  location / {
    try_files $uri /index.html;
    add_header X-Frame-Options "DENY";
    add_header X-Content-Type-Options "nosniff";
    add_header Referrer-Policy "strict-origin-when-cross-origin";
  }

  location ~ /\. {
    deny all;
  }
}
```

- [ ] **Step 3: Create `docker/config.json.template`**

Create `clients/dashboard/docker/config.json.template`:

```json
{
  "apiBase": "${FSH_API_URL}",
  "defaultTenant": "${FSH_DEFAULT_TENANT}"
}
```

- [ ] **Step 4: Create the entrypoint script**

Create `clients/dashboard/docker/docker-entrypoint.sh`:

```bash
#!/bin/sh
set -e

: "${FSH_API_URL:?FSH_API_URL is required (e.g. https://api.example.com)}"
: "${FSH_DEFAULT_TENANT:=root}"

export FSH_API_URL FSH_DEFAULT_TENANT

envsubst < /usr/share/nginx/html/config.json.template \
       > /usr/share/nginx/html/config.json
rm /usr/share/nginx/html/config.json.template

exec nginx -g 'daemon off;'
```

- [ ] **Step 5: Create the multi-stage Dockerfile**

Create `clients/dashboard/Dockerfile`:

```dockerfile
# syntax=docker/dockerfile:1.7

FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:alpine AS runtime
WORKDIR /usr/share/nginx/html
RUN apk add --no-cache gettext
RUN rm -rf ./* /etc/nginx/conf.d/default.conf
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/ ./
COPY docker/config.json.template ./config.json.template
COPY docker/docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh
EXPOSE 80
ENTRYPOINT ["/docker-entrypoint.sh"]
```

- [ ] **Step 6: Build and smoke-test**

```bash
cd clients/dashboard
docker build -t fsh/dashboard:smoke .
docker run --rm -d --name fsh-dashboard-smoke \
  -p 18082:80 \
  -e FSH_API_URL=http://localhost:5000 \
  fsh/dashboard:smoke
sleep 2
curl -fsS http://localhost:18082/config.json
```
Expected:
```json
{
  "apiBase": "http://localhost:5000",
  "defaultTenant": "root"
}
```

```bash
docker rm -f fsh-dashboard-smoke
```

- [ ] **Step 7: Commit**

```bash
git add clients/dashboard/Dockerfile clients/dashboard/.dockerignore clients/dashboard/docker/
git commit -m "build(dashboard): Dockerfile with nginx + runtime /config.json envsubst"
```

---

### Task 6: API Dockerfile — chiseled runtime + csproj hardening

**Files:**
- Modify: `src/Host/FSH.Starter.Api/Dockerfile`
- Modify: `src/Host/FSH.Starter.Api/FSH.Starter.Api.csproj`

The existing API Dockerfile uses `mcr.microsoft.com/dotnet/sdk:10.0-preview` and `mcr.microsoft.com/dotnet/aspnet:10.0-preview` (preview tags, full base, not chiseled). Update to GA tags + chiseled runtime + faster restore layering.

- [ ] **Step 1: Add `<ContainerUser>` and `<ContainerFamily>` to the API csproj**

Open `src/Host/FSH.Starter.Api/FSH.Starter.Api.csproj` and find the first `<PropertyGroup>` (the one containing `<TargetFramework>`). Add two lines inside it:

```xml
<ContainerUser>$APP_UID</ContainerUser>
<ContainerFamily>noble-chiseled</ContainerFamily>
```

These are honored by the SDK's `PublishContainer` target — they don't affect the hand-written Dockerfile but keep the GHCR-published image consistent with the compose-built one.

- [ ] **Step 2: Verify the .NET solution still builds**

```bash
dotnet build src/Host/FSH.Starter.Api/FSH.Starter.Api.csproj --nologo -clp:NoSummary -v q
```
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`.

- [ ] **Step 3: Replace the API Dockerfile**

Overwrite `src/Host/FSH.Starter.Api/Dockerfile`:

```dockerfile
# syntax=docker/dockerfile:1.7

# ── Build stage ─────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Layer 1: solution + Directory.* files first so dotnet restore can be
# cached across source-only edits.
COPY src/FSH.Starter.slnx src/Directory.Build.props src/Directory.Packages.props src/Directory.Build.targets src/global.json ./src/
COPY src/.editorconfig ./src/

# Layer 2: csproj-only copy so restore is independent of source files.
# A glob-copy here preserves directory structure for `dotnet restore`.
COPY src/ ./src/
RUN dotnet restore src/FSH.Starter.slnx

# Layer 3: publish the API
RUN dotnet publish src/Host/FSH.Starter.Api/FSH.Starter.Api.csproj \
    -c Release \
    --no-restore \
    -o /publish

# ── Runtime stage ──────────────────────────────────────────────────
# Chiseled = no shell, no apt, no debugger. Smaller pull + smaller CVE
# surface. APP_UID (1654) is the non-root user baked into the image.
FROM mcr.microsoft.com/dotnet/nightly/aspnet:10.0-noble-chiseled AS runtime
WORKDIR /app
COPY --from=build /publish .

USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "FSH.Starter.Api.dll"]
```

Notes:
- If `Directory.Build.props` / `Directory.Build.targets` / `global.json` / `.editorconfig` don't exist under `src/`, drop those lines from the first COPY — `docker build` will fail clearly if a referenced file is missing, so adjust based on the actual error.
- The second `COPY src/ ./src/` overwrites the first; this is intentional and gives us a free restore-cache layer at the cost of a few hundred KB of csproj copies.

- [ ] **Step 4: Smoke-build the API image**

```bash
cd <repo-root>   # NOT inside src/Host/FSH.Starter.Api
docker build -t fsh/api:smoke -f src/Host/FSH.Starter.Api/Dockerfile .
```
Expected: build succeeds in 2–4 minutes on cold cache.

```bash
docker image ls fsh/api:smoke --format '{{.Size}}'
```
Expected: image size around 200–300 MB (the chiseled runtime + the published bits). For reference, the old preview-non-chiseled image was ~400+ MB.

- [ ] **Step 5: Smoke-run the API in isolation**

The API will fail to start because there's no Postgres / no `JWT_SIGNING_KEY` / no `Seed:DefaultAdminPassword`. We're verifying the container can boot, hit Program.cs, and print the expected validation error.

```bash
docker run --rm fsh/api:smoke 2>&1 | head -10
```
Expected: container starts, hits `JwtOptions.Validate()`, prints something like `OptionsValidationException: ... No Key defined in JwtOptions config` (or `placeholder` if the dev config snuck in), exits non-zero. That proves the runtime works.

- [ ] **Step 6: Commit**

```bash
git add src/Host/FSH.Starter.Api/Dockerfile src/Host/FSH.Starter.Api/FSH.Starter.Api.csproj
git commit -m "build(api): chiseled runtime + APP_UID; layered restore; bump bases off preview"
```

---

### Task 7: DbMigrator Dockerfile + csproj hardening

**Files:**
- Create: `src/Host/FSH.Starter.DbMigrator/Dockerfile`
- Modify: `src/Host/FSH.Starter.DbMigrator/FSH.Starter.DbMigrator.csproj`

DbMigrator has no Dockerfile today. Same shape as the API.

- [ ] **Step 1: Add the same two csproj lines**

Open `src/Host/FSH.Starter.DbMigrator/FSH.Starter.DbMigrator.csproj` and add to the first `<PropertyGroup>`:

```xml
<ContainerUser>$APP_UID</ContainerUser>
<ContainerFamily>noble-chiseled</ContainerFamily>
```

- [ ] **Step 2: Build to verify**

```bash
dotnet build src/Host/FSH.Starter.DbMigrator/FSH.Starter.DbMigrator.csproj --nologo -clp:NoSummary -v q
```
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`.

- [ ] **Step 3: Create the DbMigrator Dockerfile**

Create `src/Host/FSH.Starter.DbMigrator/Dockerfile`:

```dockerfile
# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/ ./src/
RUN dotnet restore src/FSH.Starter.slnx
RUN dotnet publish src/Host/FSH.Starter.DbMigrator/FSH.Starter.DbMigrator.csproj \
    -c Release \
    --no-restore \
    -o /publish

FROM mcr.microsoft.com/dotnet/nightly/aspnet:10.0-noble-chiseled AS runtime
WORKDIR /app
COPY --from=build /publish .

USER $APP_UID
# Migrator reads DOTNET_ENVIRONMENT to pick appsettings.<env>.json overrides.
ENV DOTNET_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "FSH.Starter.DbMigrator.dll"]
# Default command: apply migrations. Compose overrides with `command: ["apply"]`
# explicitly so it's grep-able in the compose file.
CMD ["apply"]
```

Note: aspnet runtime is used (not the smaller `runtime`) so the chiseled image gives us a consistent base across API + Migrator. The DbMigrator does spin up a host (it depends on the Identity module for the seeder) so the aspnet base is correct.

- [ ] **Step 4: Smoke-build**

```bash
docker build -t fsh/dbmigrator:smoke -f src/Host/FSH.Starter.DbMigrator/Dockerfile .
```
Expected: build succeeds.

- [ ] **Step 5: Smoke-run (will fail with no Postgres)**

```bash
docker run --rm fsh/dbmigrator:smoke 2>&1 | head -5
```
Expected: prints `[migrator] FAILED: DatabaseOptions:ConnectionString is empty — refusing to run against an unconfigured target.` and exits 1. That's the expected fail-fast guard already in the migrator's Program.cs.

- [ ] **Step 6: Commit**

```bash
git add src/Host/FSH.Starter.DbMigrator/Dockerfile src/Host/FSH.Starter.DbMigrator/FSH.Starter.DbMigrator.csproj
git commit -m "build(migrator): chiseled Dockerfile + APP_UID + csproj container hardening"
```

---

### Task 8: `deploy/docker/.env.example`

**Files:**
- Create: `deploy/docker/.env.example`

- [ ] **Step 1: Write the env example**

Create `deploy/docker/.env.example`:

```dotenv
# ──────────────────────────────────────────────────────────────────────
# FullStackHero — production docker-compose configuration.
# Copy to `.env` and edit. NEVER commit .env to git.
# Compose refuses to start until every required (?-flagged) var is set.
# ──────────────────────────────────────────────────────────────────────

# ── Public URLs ──────────────────────────────────────────────────────
# Where each surface is reachable from end users' browsers (via your
# external proxy / TLS terminator). Used by:
#   • frontends, baked into /config.json at container start
#   • API CORS allow-list for the two frontend origins
FSH_API_URL=https://api.example.com
FSH_ADMIN_URL=https://admin.example.com
FSH_DASHBOARD_URL=https://app.example.com

# Default tenant identifier used by the login forms on first paint.
FSH_DEFAULT_TENANT=root

# ── Host ports for the external proxy to point at ───────────────────
# Only FSH services publish ports. Postgres/Redis/MinIO stay
# compose-internal by default (uncomment their `ports:` blocks in
# docker-compose.yml if you need host access for psql / redis-cli).
FSH_API_PORT=8080
FSH_ADMIN_PORT=8081
FSH_DASHBOARD_PORT=8082

# ── Secrets ─────────────────────────────────────────────────────────
# JWT signing key — must be 32+ chars and NOT contain the substring
# "replace-with" (the framework's placeholder detector blocks that).
# Generate with:    openssl rand -base64 48
JWT_SIGNING_KEY=

# Initial root admin password the seeder uses on first boot. Sign in
# at https://admin.example.com with admin@root.com using this value,
# then rotate from Settings → Security immediately. Required.
SEED_ADMIN_PASSWORD=

# ── Data plane (defaults are fine for self-hosted compose) ──────────
POSTGRES_PASSWORD=
REDIS_PASSWORD=
MINIO_ROOT_USER=
MINIO_ROOT_PASSWORD=

# ── Observability (optional) ────────────────────────────────────────
# Point at an OTLP collector to ship traces/metrics. Leave blank to
# disable export. Example: http://otel-collector:4317
OTEL_EXPORTER_OTLP_ENDPOINT=
```

- [ ] **Step 2: Commit**

```bash
git add deploy/docker/.env.example
git commit -m "build(deploy): docker-compose .env.example with all knobs documented"
```

---

### Task 9: Postgres init SQL

**Files:**
- Create: `deploy/docker/postgres-init/01-create-databases.sql`

Postgres's official image runs every `*.sql` and `*.sh` in `/docker-entrypoint-initdb.d/` on first start (only when the data volume is empty). We use this to set up extensions and confirm the database. The `POSTGRES_DB=fsh` env in compose already creates the database itself, so this is just extensions + a sanity note.

- [ ] **Step 1: Write the init SQL**

Create `deploy/docker/postgres-init/01-create-databases.sql`:

```sql
-- Postgres init for fullstackhero
-- This file runs only on first boot (when /var/lib/postgresql/data is empty).
-- The `fsh` database itself is created by POSTGRES_DB= in the compose env;
-- we just add the extensions the framework relies on.

\connect fsh

CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- pg_trgm is used by the full-text search GIN indexes (chat search).
CREATE EXTENSION IF NOT EXISTS pg_trgm;
```

- [ ] **Step 2: Commit**

```bash
git add deploy/docker/postgres-init/01-create-databases.sql
git commit -m "build(deploy): postgres init SQL — required extensions"
```

---

### Task 10: `deploy/docker/docker-compose.yml`

**Files:**
- Create: `deploy/docker/docker-compose.yml`

- [ ] **Step 1: Write the compose file**

Create `deploy/docker/docker-compose.yml`:

```yaml
# fullstackhero — production docker-compose.
# Run from this directory:
#   cp .env.example .env && $EDITOR .env
#   docker compose up -d --build
#
# Operator owns the edge (TLS / subdomain routing). This file publishes
# only the FSH services on host ports; the data plane (postgres/redis/
# minio) stays compose-internal unless you uncomment their ports blocks.

name: fsh

services:
  postgres:
    image: postgres:17-alpine
    container_name: fsh-postgres
    restart: unless-stopped
    environment:
      POSTGRES_DB: fsh
      POSTGRES_USER: fsh
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:?POSTGRES_PASSWORD is required}
    volumes:
      - pg_data:/var/lib/postgresql/data
      - ./postgres-init:/docker-entrypoint-initdb.d:ro
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U fsh -d fsh"]
      interval: 5s
      timeout: 3s
      retries: 12
    # Uncomment to expose for host psql / pg_dump access:
    # ports:
    #   - "5432:5432"

  redis:
    image: redis:7-alpine
    container_name: fsh-redis
    restart: unless-stopped
    command:
      - redis-server
      - --requirepass
      - ${REDIS_PASSWORD:?REDIS_PASSWORD is required}
      - --appendonly
      - "yes"
    volumes:
      - redis_data:/data
    healthcheck:
      test: ["CMD-SHELL", "redis-cli -a $$REDIS_PASSWORD PING | grep PONG"]
      interval: 5s
      timeout: 3s
      retries: 12
    environment:
      REDIS_PASSWORD: ${REDIS_PASSWORD}
    # Uncomment to expose for host redis-cli:
    # ports:
    #   - "6379:6379"

  minio:
    image: minio/minio:latest
    container_name: fsh-minio
    restart: unless-stopped
    command: ["server", "/data", "--console-address", ":9001"]
    environment:
      MINIO_ROOT_USER: ${MINIO_ROOT_USER:?MINIO_ROOT_USER is required}
      MINIO_ROOT_PASSWORD: ${MINIO_ROOT_PASSWORD:?MINIO_ROOT_PASSWORD is required}
    volumes:
      - minio_data:/data
    healthcheck:
      test: ["CMD", "mc", "ready", "local"]
      interval: 10s
      timeout: 3s
      retries: 12
    # Uncomment to expose the console (admin UI) + S3 API to the host:
    # ports:
    #   - "9000:9000"   # S3 API
    #   - "9001:9001"   # Web console

  migrator:
    build:
      context: ../..
      dockerfile: src/Host/FSH.Starter.DbMigrator/Dockerfile
    image: fsh/dbmigrator:local
    container_name: fsh-migrator
    restart: "no"
    depends_on:
      postgres: { condition: service_healthy }
    environment:
      DOTNET_ENVIRONMENT: Production
      DatabaseOptions__ConnectionString: Host=postgres;Database=fsh;Username=fsh;Password=${POSTGRES_PASSWORD}
      CachingOptions__Redis: redis,password=${REDIS_PASSWORD}
      JwtOptions__SigningKey: ${JWT_SIGNING_KEY:?JWT_SIGNING_KEY is required}
      Seed__DefaultAdminPassword: ${SEED_ADMIN_PASSWORD:?SEED_ADMIN_PASSWORD is required}
    command: ["apply"]

  api:
    build:
      context: ../..
      dockerfile: src/Host/FSH.Starter.Api/Dockerfile
    image: fsh/api:local
    container_name: fsh-api
    restart: unless-stopped
    depends_on:
      postgres: { condition: service_healthy }
      redis:    { condition: service_healthy }
      minio:    { condition: service_healthy }
      migrator: { condition: service_completed_successfully }
    environment:
      DOTNET_ENVIRONMENT: Production
      ASPNETCORE_URLS: http://+:8080
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
      CorsOptions__AllowedOrigins__0: ${FSH_ADMIN_URL:?FSH_ADMIN_URL is required}
      CorsOptions__AllowedOrigins__1: ${FSH_DASHBOARD_URL:?FSH_DASHBOARD_URL is required}
      OpenTelemetryOptions__Exporter__Otlp__Endpoint: ${OTEL_EXPORTER_OTLP_ENDPOINT:-}
    ports:
      - "${FSH_API_PORT:-8080}:8080"

  admin:
    build:
      context: ../../clients/admin
    image: fsh/admin:local
    container_name: fsh-admin
    restart: unless-stopped
    depends_on:
      migrator: { condition: service_completed_successfully }
    environment:
      FSH_API_URL: ${FSH_API_URL:?FSH_API_URL is required}
      FSH_DASHBOARD_URL: ${FSH_DASHBOARD_URL}
      FSH_DEFAULT_TENANT: ${FSH_DEFAULT_TENANT:-root}
    ports:
      - "${FSH_ADMIN_PORT:-8081}:80"

  dashboard:
    build:
      context: ../../clients/dashboard
    image: fsh/dashboard:local
    container_name: fsh-dashboard
    restart: unless-stopped
    depends_on:
      migrator: { condition: service_completed_successfully }
    environment:
      FSH_API_URL: ${FSH_API_URL}
      FSH_DEFAULT_TENANT: ${FSH_DEFAULT_TENANT:-root}
    ports:
      - "${FSH_DASHBOARD_PORT:-8082}:80"

volumes:
  pg_data:
  redis_data:
  minio_data:
```

- [ ] **Step 2: Validate the compose file parses**

```bash
cd deploy/docker
cp .env.example .env
# Fill the required vars with placeholder values just for parse validation
sed -i.bak \
    -e 's|^JWT_SIGNING_KEY=|JWT_SIGNING_KEY=parse-validation-only-32+-chars-here!|' \
    -e 's|^SEED_ADMIN_PASSWORD=|SEED_ADMIN_PASSWORD=ParseOnly123!|' \
    -e 's|^POSTGRES_PASSWORD=|POSTGRES_PASSWORD=pg-parse|' \
    -e 's|^REDIS_PASSWORD=|REDIS_PASSWORD=redis-parse|' \
    -e 's|^MINIO_ROOT_USER=|MINIO_ROOT_USER=minioparse|' \
    -e 's|^MINIO_ROOT_PASSWORD=|MINIO_ROOT_PASSWORD=minio-parse-pwd|' \
    .env
docker compose config --quiet
echo "exit: $?"
```
Expected: `exit: 0`, no parse errors, no missing-var errors.

- [ ] **Step 3: Clean up the parse-test .env**

```bash
rm -f .env .env.bak
```

- [ ] **Step 4: Commit**

```bash
git add deploy/docker/docker-compose.yml
git commit -m "build(deploy): production docker-compose.yml — full stack on one host"
```

---

### Task 11: `deploy/docker/README.md`

**Files:**
- Create: `deploy/docker/README.md`

- [ ] **Step 1: Write the deploy guide**

Create `deploy/docker/README.md`:

````markdown
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
curl -fsS http://localhost:8080/health        # API
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
````

- [ ] **Step 2: Commit**

```bash
git add deploy/docker/README.md
git commit -m "docs(deploy): five-minute docker-compose deploy guide"
```

---

### Task 12: End-to-end smoke

The earlier tasks each did per-service smoke. This task brings the whole stack up against a real `.env` and verifies the happy path.

- [ ] **Step 1: Prepare a real `.env`**

```bash
cd deploy/docker
cp .env.example .env
# Generate real secrets
JWT_KEY=$(openssl rand -base64 48)
ADMIN_PW="Smoke123!Pass"
PG_PW=$(openssl rand -hex 16)
REDIS_PW=$(openssl rand -hex 16)
MINIO_PW=$(openssl rand -hex 16)
sed -i.bak \
  -e "s|^JWT_SIGNING_KEY=.*|JWT_SIGNING_KEY=${JWT_KEY}|" \
  -e "s|^SEED_ADMIN_PASSWORD=.*|SEED_ADMIN_PASSWORD=${ADMIN_PW}|" \
  -e "s|^POSTGRES_PASSWORD=.*|POSTGRES_PASSWORD=${PG_PW}|" \
  -e "s|^REDIS_PASSWORD=.*|REDIS_PASSWORD=${REDIS_PW}|" \
  -e "s|^MINIO_ROOT_USER=.*|MINIO_ROOT_USER=fshminio|" \
  -e "s|^MINIO_ROOT_PASSWORD=.*|MINIO_ROOT_PASSWORD=${MINIO_PW}|" \
  -e 's|^FSH_API_URL=.*|FSH_API_URL=http://localhost:8080|' \
  -e 's|^FSH_ADMIN_URL=.*|FSH_ADMIN_URL=http://localhost:8081|' \
  -e 's|^FSH_DASHBOARD_URL=.*|FSH_DASHBOARD_URL=http://localhost:8082|' \
  .env
rm .env.bak
```

(For smoke we point at localhost; in real deploy the operator points at their subdomains.)

- [ ] **Step 2: Bring the stack up clean**

```bash
docker compose down -v 2>/dev/null
docker compose up -d --build
```
Expected: builds 4 images (1st run), then containers start. ~5 min cold.

- [ ] **Step 3: Wait for the migrator to complete**

```bash
docker compose wait migrator
echo "exit: $?"
```
Expected: `exit: 0`. (`docker compose wait` blocks until the named service exits and returns its exit code.)

- [ ] **Step 4: Smoke the surfaces**

```bash
curl -fsS http://localhost:8080/health
```
Expected: a JSON `{"status":"Healthy", ...}` body.

```bash
curl -fsS http://localhost:8081/config.json
```
Expected:
```json
{ "apiBase": "http://localhost:8080", "defaultTenant": "root", "dashboardUrl": "http://localhost:8082" }
```

```bash
curl -fsS http://localhost:8082/config.json
```
Expected:
```json
{ "apiBase": "http://localhost:8080", "defaultTenant": "root" }
```

```bash
curl -fsSI http://localhost:8081/ | head -1
curl -fsSI http://localhost:8082/ | head -1
```
Expected: both `HTTP/1.1 200 OK`.

- [ ] **Step 5: Verify login works end-to-end**

```bash
TOKEN=$(curl -fsS -X POST http://localhost:8080/api/v1/identity/token/issue \
  -H 'content-type: application/json' \
  -H 'tenant: root' \
  -d "{\"email\":\"admin@root.com\",\"password\":\"${ADMIN_PW}\"}" \
  | jq -r .accessToken)
echo "got token: ${TOKEN:0:40}…"

curl -fsS http://localhost:8080/api/v1/identity/profile \
  -H "authorization: Bearer ${TOKEN}" \
  -H 'tenant: root' \
  | jq '.email'
```
Expected: token prints, profile call returns `"admin@root.com"`.

- [ ] **Step 6: Verify volume durability**

```bash
docker compose down
docker compose up -d
# After data services come up healthy, migrator should re-run and exit 0
# WITHOUT re-seeding (idempotent).
docker compose logs migrator | tail -10
```
Expected: migrator runs, exits 0, no errors. The data from step 5 is still there.

- [ ] **Step 7: Tear down + cleanup**

```bash
docker compose down -v
rm .env
```

- [ ] **Step 8: Commit (no file changes — placeholder marker)**

```bash
# No code changes from this task. If you want a marker commit, allow-empty:
git commit --allow-empty -m "test(deploy): end-to-end docker-compose smoke verified locally"
```

---

### Task 13: Point root README at the deploy guide

**Files:**
- Modify: `README.md` (repo root)

- [ ] **Step 1: Find an appropriate spot in the README**

Open `README.md`. Look for an existing "Getting started" / "Quick start" / "Deploy" section. If none exists, add a new section near the top (after the project tagline / badges).

- [ ] **Step 2: Add a "Deploy" section**

Insert:

```markdown
## Deploy

Production-style single-host deployment via Docker Compose:

```bash
cd deploy/docker
cp .env.example .env && $EDITOR .env
docker compose up -d --build
```

Full walkthrough in [`deploy/docker/README.md`](deploy/docker/README.md).
```

If the existing README already has a "Quick start" section that targets `dotnet run`, keep it — that's the local-dev story. The Deploy section is the production-ship story.

- [ ] **Step 3: Commit**

```bash
git add README.md
git commit -m "docs: point README at deploy/docker for the production deploy story"
```

---

### Task 14: Push the branch

- [ ] **Step 1: Verify the branch state**

```bash
git status
git log --oneline -15
```
Expected: clean working tree; 12–13 new commits since `origin/develop`.

- [ ] **Step 2: Push**

```bash
git push origin develop
```
Expected: push succeeds, prints the new range.

---

## Self-review notes

- **Spec coverage:** Every artifact in the spec's "Artifact layout" section maps to a task. The implementation order in the spec matches the task order here (`.dockerignore` → frontend boot → frontend Dockerfiles → .NET Dockerfiles → csproj hardening → env → init SQL → compose → README → smoke → root README).
- **Boot refactor depth:** The spec called out that env reads on first paint must see the loaded config; the env.ts implementations use getters that throw if `loadRuntimeConfig()` didn't run, catching accidental import-time reads at runtime.
- **Hardening bundled:** `<ContainerUser>$APP_UID</ContainerUser>` and `<ContainerFamily>noble-chiseled</ContainerFamily>` land in API + Migrator csproj files in tasks 6 and 7. The hand-written Dockerfiles also use the chiseled runtime + `USER $APP_UID` so the compose-built images stay aligned with the SDK-PublishContainer images CI publishes.
- **Fail-fast:** Compose uses `:?` syntax for every required env var so missing values surface at compose parse time, not when the API throws six layers deep.
- **No "TODO" / "TBD" steps; every step has either a literal file write or an exact shell command + expected output.**

## Out of scope (per the spec)

- Helm chart / k8s manifests — v1.1
- Multi-arch images — separate publish-workflow PR
- Caddy / Traefik / built-in TLS — operator owns the edge
- Localhost demo overlay — Aspire is the local-dev path
