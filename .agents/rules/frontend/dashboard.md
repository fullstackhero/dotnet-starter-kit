# Frontend — dashboard app (`clients/dashboard`)

Tenant-facing application. Read `frontend/shared.md` first; this file is only the divergences.

- **Port** 5174 · dev proxy target `https://localhost:7030` (HTTPS, with `ws: true` for the SignalR hub) · localStorage prefix `fsh.dashboard.*` · login header `X-FSH-App: dashboard`.
- **Env** (`src/env.ts`): `{ apiBase, defaultTenant, demoMode }`.
- Dev-proxy is HTTPS on purpose: routing the bearer token through an HTTP→HTTPS 307 redirect stripped the `Authorization` header.

## No RHF/zod — hand-rolled forms

The dashboard does **not** depend on react-hook-form or zod. Use controlled inputs + local state. Don't add those deps to match admin.

## Permissions — straight from the JWT

`auth-context.tsx` reads `claims.permissions` off the decoded JWT — no separate fetch, no `permissionsHydrated`, no permissions cache key. `ProtectedRoute` is **auth-only** (no permission gating). Don't add `RouteGuard`-style gating here.

## Routing & realtime/SSE

- Every route element is wrapped in `withSuspense(node)` (per-route skeleton fallback). No per-route permission guards.
- `RealtimeProvider` **and** `SseProvider` are mounted inside `AppShell` (authenticated routes only), under a `CommandPaletteProvider` (cmdk).
- SignalR provider pre-wires ~11 chat/notification events.
- **SSE** (`src/sse/`, dashboard-only): two-step token — `POST /api/v1/sse/token`, then `GET /api/v1/sse/stream?token=<guid>` consumed via fetch streaming (`parseSseStream` async generator; EventSource can't send auth headers). **Two split contexts:** `useSseStatus()` (stable, for status dots) vs `useSseEvents()` (mutates per event) to avoid cascading re-renders; `useSse()` is the composite.

## Impersonation

`token-store.ts` has `beginImpersonation` / `endImpersonationWithFreshTokens` / `restoreStashedActor` that stash the operator's tokens under `fsh.dashboard.impersonation.*`. `AuthProvider` exposes `beginImpersonation`/`stopImpersonation` and derives `ImpersonationInfo` from `act_sub` / `act_tenant` / `act_name` claims. Admin triggers the handoff one-way via its `dashboardUrl`.

## Performance

- `@tanstack/react-virtual` for long lists — use it for any large collection (chat history, big tables).
- `cmdk` powers the command palette.

## Theme

**Chroma-0 neutrals** (`--neutral-*: oklch(L 0 0)` — untinted; the warm-paper tint was deliberately removed). Rose default brand with **swappable accent themes** via `.accent-{rose,indigo,violet,sky,emerald,amber}` classes that override the `--brand-*` oklch stops; saffron secondary; Figtree font. Defined in `src/styles/globals.css`. Keep neutrals at chroma 0.

## Add-a-page deltas (on top of shared steps)

- Hand-roll forms (no RHF/zod).
- Wrap the route element in `withSuspense(<X/>)`; no permission guard.
- If it consumes pushes: `useRealtimeEvent("EventName", handler)` (register the name in `realtime-context.tsx`) or `useSseEvents()` for SSE.
- Use `react-virtual` for long lists; keep neutrals chroma 0.
