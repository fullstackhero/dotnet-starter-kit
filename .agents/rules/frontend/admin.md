# Frontend — admin app (`clients/admin`)

Operator/SuperAdmin-facing console. Read `frontend/shared.md` first; this file is only the divergences.

- **Port** 5173 · dev proxy target `http://localhost:5030` (HTTP) · localStorage prefix `fsh.admin.*` · login header `X-FSH-App: admin`.
- **Env** (`src/env.ts`): `{ apiBase, defaultTenant, dashboardUrl }`. `dashboardUrl` is used for the one-way impersonation handoff into the dashboard app.

## Forms — react-hook-form + zod

Admin is the **only** app with `react-hook-form` + `zod` + `@hookform/resolvers`. Use them:

```ts
const form = useForm<Schema>({ resolver: zodResolver(schema) });
```

Form layout primitives live in `src/components/list/` (`PageHeader`, `Field`, `FormShell`, `FormSection`, `FormActions`, `Pagination`, `ErrorBand`).

## Permissions — fetched, hydrated, gated

- The JWT carries only role names. Admin fetches the permission set separately: `GET /api/v1/identity/permissions` (`getMyPermissions`), cached under `fsh.admin.permissions`.
- `AuthProvider` hydrates them in an effect keyed on subject change and exposes `permissionsHydrated` to avoid a 403 flash on first paint.
- **Route gating:** wrap gated route elements in `<RouteGuard perms={[IdentityPermissions.Users.View]}>…</RouteGuard>`. It renders a "Resolving permissions" state while `!permissionsHydrated`, else `<ForbiddenView missing={…}/>`. (`ProtectedRoute` also accepts a `permissions?` prop.)
- **Mirror server permissions by hand** in `src/lib/permissions.ts` (`IdentityPermissions`, `MultitenancyPermissions`, … frozen objects + `PERMISSION_CATALOG` driving the role editor). There is intentionally **no** runtime catalog fetch — when the server adds a permission, mirror the constant here.

## Routing & realtime

- Routes wrap elements in `<RouteGuard perms={…}>` (no per-route Suspense wrapper).
- `RealtimeProvider` is mounted in `App.tsx` and wires only `["NotificationCreated"]`.

## Theme

Cool-cast neutrals (hue 240, small non-zero chroma — **not** chroma 0), a single fixed chartreuse "signal" accent (`--accent-signal` / `--signal-500`), Geist / Geist Mono fonts. Defined in `src/styles/globals.css`.

## Add-a-page deltas (on top of shared steps)

- Use RHF + zod for any form.
- If the endpoint requires a permission, mirror the constant in `src/lib/permissions.ts` (and `PERMISSION_CATALOG` if it belongs in the role editor) and wrap the route in `<RouteGuard perms={[…]}>`.
- Playwright: `seedAuthedSession` here also pre-seeds `fsh.admin.permissions` so `RouteGuard` passes on first paint.
