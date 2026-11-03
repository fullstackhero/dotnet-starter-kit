# Frontend — shared conventions

Applies to **both** `clients/admin` and `clients/dashboard`. Read this for any React work, then read
the app-specific file (`admin.md` / `dashboard.md`) for divergences.

Stack: React 19 · Vite 7 · TypeScript · TanStack Query v5 · React Router 7 · Radix UI · Tailwind v4 ·
class-variance-authority (shadcn-style) · `@microsoft/signalr`. Path alias `@` → `src` (`vite.config.ts`).

## API client (`src/lib/api-client.ts`, `src/api/*`)

- One fetch wrapper: `apiFetch<T>(path, init)`. No axios.
- **Types are hand-written**, not generated. (`openapi-typescript` is declared in admin's devDeps but unused — there is no codegen step.) Define DTO `type`s and a `const BASE = "/api/v1/..."` in each `src/api/{feature}.ts`, with thin async functions calling `apiFetch`.
- Auth header `Authorization: Bearer <access>` from `tokenStore.getAccessToken()` (unless `skipAuth:true`).
- Tenant header (lowercase `tenant`) = `tokenStore.getTenant() ?? env.defaultTenant`, unless overridden per request.
- Errors: non-OK parses RFC 9457 `application/problem+json` and throws `ApiRequestError(status, message, problem)`. 204/empty → `undefined`.
- **Single-flight refresh:** on 401 with a refresh token, `POST /api/v1/identity/token/refresh` with `{token, refreshToken}`; a module-level `refreshPromise` dedupes concurrent refreshes; rotated token returns on `token` (not `accessToken`); the original request retries once.
- Search endpoints: build `URLSearchParams` with **PascalCase** keys (`PageNumber`, `PageSize`, …) to match the API.

## Env (`src/env.ts`) — runtime, not build-time

`loadRuntimeConfig()` fetches `/config.json` once at boot (awaited in `main.tsx` before React mounts); `env` is a getter that throws if read too early. One built image promotes across environments (operator writes `config.json`). The only `VITE_*` var, `VITE_API_BASE_URL`, configures the **Vite dev proxy target only** — it is not the runtime apiBase (`config.json` ships `apiBase: ""`, relative).

## Data fetching (TanStack Query v5)

- Shared `queryClient` (`src/lib/query-client.ts`): `staleTime: 30_000`, `refetchOnWindowFocus:false`, no retry on 401/403 else `failureCount < 2`.
- **Query keys are inline literal arrays**, hierarchical, params object last: `["users", {pageNumber, searchTerm}]`, `["user", id]`, `["user", id, "roles"]`. No central key factory.
- `useQuery`/`useMutation` live inline in page components. Invalidate in `onSuccess`: `queryClient.invalidateQueries({ queryKey: ["users"] })`. Pagination: `placeholderData: keepPreviousData`.

### ⚠️ The `mutate(arg)` race-safe pattern (golden rule #9)

`useMutation` reads its options at execute time, so values produced at call time (e.g. a fresh
`crypto.randomUUID()` client id) must ride **through `mutate(arg)`** and be read from the `variables`
argument of `onMutate`/`onSuccess`/`onError` — never from component state the callbacks close over,
or two rapid calls collide.

```ts
mutation.mutate({ text, clientId: crypto.randomUUID() });
// onMutate: ({ clientId }) => insert optimistic `temp:${clientId}`
// onSuccess: (real, { clientId }) => swap temp → real
// onError:   (_e, { clientId }) => rollback
```

## Routing (`routes.tsx`, `App.tsx`)

- `createBrowserRouter`, flat config. Pages are **named exports** loaded via a `lazyNamed(importer, name)` helper (adapts named → `React.lazy`'s default contract). No default exports.
- Nesting: public auth routes → `<ProtectedRoute/>` → `<AppShell/>` → page children. `errorElement: <RouteError/>`.
- Provider tree: `ThemeProvider > QueryClientProvider > AuthProvider > … > RouterProvider` + `sonner` `<Toaster/>`.

## Auth (`src/auth/`)

`token-store.ts` (localStorage + pub/sub), `jwt.ts` (`decodeJwt`), `AuthProvider`/`useAuth()`, `ProtectedRoute`.
Login `POST /api/v1/identity/token/issue` with header `X-FSH-App: "admin"|"dashboard"`. **localStorage keys are namespaced per app** (`fsh.admin.*` / `fsh.dashboard.*`) so both run side-by-side. Permission *source* differs per app — see the app files.

## Design system (Tailwind v4, shadcn-style)

- **`cn()` is at `src/lib/cn.ts`** (`twMerge(clsx(...))`) — not `lib/utils.ts`. `components.json`: `style:new-york`, `baseColor:slate`, `cssVariables:true`, `iconLibrary:lucide`.
- UI primitives in `src/components/ui/` are cva-based: `cva(base, { variants, defaultVariants })` + Radix `Slot`/`asChild` + `cn(buttonVariants({...}))`. Layout primitives in `src/components/list/` (admin) / similar (dashboard), re-exported from `index.ts`.
- **Tailwind v4 is CSS-first — there is NO `tailwind.config`.** Configured via the `@tailwindcss/vite` plugin and one entrypoint `src/styles/globals.css` (imported in `main.tsx`). Tokens: `:root` oklch primitives → semantic vars → an `@theme inline { --color-*: var(--…) }` block exposing them as utilities. `@custom-variant dark (&:is(.dark *))`.
- Add a new token in `globals.css` (primitive → semantic → `@theme inline`), then use the utility. Don't hard-code colors in components.

### Design language (both apps differ on purpose)

| | admin (operator) | dashboard (tenant) |
|---|---|---|
| Neutrals | cool-cast, hue 240, small non-zero chroma | **chroma 0** (untinted — the warm tint was removed) |
| Accent | single fixed chartreuse "signal" (`--accent-signal`) | rose brand + **swappable** `.accent-{rose,indigo,violet,sky,emerald,amber}` |
| Font | Geist / Geist Mono | Figtree (+ saffron secondary) |

So "neutrals must be chroma 0" is a **dashboard** rule. Admin neutrals are intentionally cool — match the file you're editing.

## Realtime (SignalR)

`src/realtime/realtime-context.tsx`: one `HubConnection` to `/api/v1/realtime/hub`. `@microsoft/signalr` is **dynamically imported** (lazy ~37KB) only when an authed session opens the hub. Auth via `accessTokenFactory`; a `tokenEpoch` (bumped by `tokenStore.subscribe`) forces reconnect on login/refresh/impersonation. Consume with `useRealtimeEvent("EventName", handler, deps)` (handler kept in a ref). Pre-register new event names in the provider's event list.

## Translations (i18next)

Every string a user reads goes through `t()`; English and Portuguese ship today. The backend negotiates its own messages from the `Accept-Language` that `apiFetch` sends, so ProblemDetails text is shown as-is (`.agents/rules/localization.md`).

- **One namespace per area**: `src/locales/en-US/{ns}.json`, registered with one line in the catalog map in `src/i18n.ts` (`CATALOGS` in admin, `catalogs` in dashboard). `src/locales/pt-BR/{ns}.json` is optional: `src/locales/pt-BR.ts` picks every file up into one lazy chunk, and a missing key renders in English.
- **Keys are flat and dotted** inside the file (`"header.title"`, `"create.validation.url"`). Interpolate with `{{name}}`; counts use `_one` / `_other` plus `{{count, number}}` and are called with `{ count }`.
- **In a component**: `const { t } = useTranslation("ns")`. Another namespace through the list form, `useTranslation(["activity", "audits"])` with `t("audits:…")`, or through a second translator when a helper requires one bound to it (`const { t: tCommon } = useTranslation("common")` for `describeUploadError`). Never prefix a key with the namespace the translator is already bound to.
- **Keys are typed.** `src/i18next.d.ts` types every key against the en-US catalogs, so add the key to the JSON first: a key the namespace lacks fails `tsc -b`. A helper that receives the translator takes `TFunction<"ns">`; a key stored in data (nav items, tabs, enum labels) is `ParseKeys<"ns">`, and an enum label goes through a map typed against the enum (`satisfies Record<Status, ParseKeys<"ns">>`) rather than a template key, so a new member without a label stops compiling. Billing and impersonation statuses have their maps in `src/lib/status-keys.ts`, read through `statusKey()`, which returns `undefined` for a status added after the build so the caller can show the raw value; `tests/i18n/status-keys.spec.ts` checks every member of the backend enum against those maps and both catalogs. A template key still compiles when the call passes `defaultValue`, but its key is not checked; a few remain.
- **The lint blocks untranslated JSX text** (`i18next/no-literal-string`, `jsx-text-only`, in `src/pages` and `src/components`). Text inside `<code>`/`<kbd>`, glyph-only runs, the product name and the build tag are allowed by pattern in `eslint.config.js`; anything else is moved to a catalog, never silenced with `eslint-disable`. It does not see attribute values (`aria-label`, `placeholder`, `title`), strings outside JSX (`toast.success("…")`, zod messages), or JSX outside those two folders, so those stay a review item.
- **Tests:** `tests/i18n/parity.spec.ts` fails on a pt-BR key en-US lacks or on different `{{variables}}` between the two; a pt-BR key may be missing.

## Testing (Playwright, route-mocked)

- `playwright.config.ts`: `testDir: ./tests`, chromium, auto-boots `npm run dev`, no real backend.
- Tests in `tests/{area}/{name}.spec.ts`; helpers in `tests/helpers/`.
- **JWT seeding:** `seedAuthedSession(page, TEST_USER)` builds a fake JWT and `addInitScript`-writes `fsh.{app}.*` to localStorage before React boots (server isn't called, so signature is junk).
- **Route mocking:** `mockJsonResponse(page, urlGlob, body)` / `mockProblemDetails(...)`. `installShellMocks(page)` stubs every call `AppShell` fires and **aborts** SSE/SignalR. Playwright matches most-recently-registered first → broad shell mocks in `beforeEach`, page-specific mocks after (they win).
- `beforeEach`: `seedAuthedSession(page, TEST_USER)` → `installShellMocks(page)`.

## Add a page/feature (shared steps)

1. API: extend `src/api/{feature}.ts` — hand-written types + `apiFetch` calls.
2. Page: `src/pages/{area}/{name}.tsx`, **named** export. `useQuery` with hierarchical key; `useMutation` invalidating in `onSuccess`, passing per-call data via `mutate(arg)`. Text through `t()` from the area's namespace, with the keys added to `src/locales/en-US/{ns}.json` (see Translations).
3. Route: add `const X = lazyNamed(() => import("@/pages/area/name"), "XPage")` and a child route under `AppShell`.
4. Test: `tests/{area}/{name}.spec.ts` with seed + shell mocks + page mocks.

Then apply the app-specific steps in `admin.md` / `dashboard.md` (forms, permission gating, suspense, etc.).
