# FullStackHero — Admin

Operator console for the FullStackHero .NET Starter Kit. Built with React 19, Vite 7, TypeScript, TanStack Query, React Router and Tailwind 4 + shadcn/ui.

This is a **standalone Vite app** — not part of a pnpm workspace — so it can be mounted into .NET Aspire as a plain `ExecutableResource` without monorepo friction.

## Prerequisites

- Node.js 20+
- The API running locally (`dotnet run --project src/Host/FSH.Starter.Api`, defaults to `http://localhost:5030`)

## Install & run

Two options — pick whichever matches how you want to develop.

### Option A — run everything through Aspire (recommended)

The AppHost launches Postgres, Redis, MinIO, the API, **and** this Vite app together, with `VITE_API_BASE_URL` wired via service discovery.

```bash
npm install --prefix clients/admin   # one-time
dotnet run --project src/Host/FSH.Starter.AppHost
```

Aspire dashboard will expose `fsh-admin` on <http://localhost:5173>.

### Option B — run the frontend standalone

Useful when the API is already running elsewhere (container, remote).

```bash
cd clients/admin
npm install
npm run dev          # http://localhost:5173
```

The dev server proxies `/api`, `/openapi`, and `/scalar` to `VITE_API_BASE_URL` (default `http://localhost:5030`), so browser requests stay same-origin.

## Scripts

| Script            | Purpose                              |
|-------------------|--------------------------------------|
| `npm run dev`     | Vite dev server on port 5173         |
| `npm run build`   | `tsc -b` + `vite build` → `dist/`    |
| `npm run preview` | Preview the production build         |
| `npm run lint`    | ESLint (flat config)                 |

## Configuration

Environment variables are read via `import.meta.env` and surfaced through `src/env.ts`:

| Variable                  | Default                  | Purpose                                       |
|---------------------------|--------------------------|-----------------------------------------------|
| `VITE_API_BASE_URL`       | `http://localhost:5030`  | API origin used by the dev proxy              |
| `VITE_DEFAULT_TENANT`     | `root`                   | Default tenant header for unauthenticated calls |

Create `.env.local` to override locally.

## Structure

```
src/
├── api/                # Typed API client functions (per backend feature)
├── auth/               # Token store, JWT decode, auth context, protected route
├── components/
│   ├── layout/         # Sidebar, Topbar, AppShell
│   └── ui/             # shadcn primitives (Button, Card, Input, Label, Table)
├── lib/
│   ├── api-client.ts   # fetch wrapper: auth header, tenant header, single-flight refresh
│   ├── query-client.ts # TanStack QueryClient
│   └── cn.ts           # clsx + tailwind-merge
├── pages/              # Route-level components
├── styles/globals.css  # Tailwind 4 CSS-first + shadcn CSS variables
├── App.tsx             # Provider tree (QueryClient, Auth, Router)
├── main.tsx            # React entry
└── routes.tsx          # Route definitions
```

## Authentication flow

1. `POST /api/v1/identity/token/issue` with `{ email, password }` plus `tenant` header.
2. Access + refresh tokens are stored in `localStorage` (keys prefixed `fsh.admin.`).
3. The API client attaches `Authorization: Bearer <access>` and `tenant: <slug>` on every call.
4. On `401`, a single-flight refresh call hits `POST /api/v1/identity/token/refresh`, retries the original request, and logs the user out if the refresh fails.

## Styling

- Tailwind 4 CSS-first config lives in `src/styles/globals.css` (no `tailwind.config.ts`).
- Colors use shadcn/ui oklch CSS variables; dark mode is toggled via the `.dark` class on `<html>`.
- shadcn components follow the **new-york** style; `components.json` is present for `npx shadcn add ...`.

## Adding a new page

1. Add the API function in `src/api/<feature>.ts`.
2. Add the page component in `src/pages/<feature>/<name>.tsx`.
3. Register it in `src/routes.tsx` as a child of the `AppShell` route.
4. Add a nav entry in `src/components/layout/sidebar.tsx`.

## Production build

`npm run build` emits a static bundle to `dist/`. Host it behind any static web server (nginx, Caddy, Azure Static Web Apps, CloudFront, …). Configure the reverse proxy to forward `/api/*` to the backend and serve `index.html` as the SPA fallback for unmatched routes.
