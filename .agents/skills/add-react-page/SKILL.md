---
name: add-react-page
description: Add a list+create page to a React app (clients/admin or clients/dashboard) — API module, page, lazy route, (admin) permission gate, Playwright test. Use when adding any frontend screen. See .agents/rules/frontend/.
argument-hint: [admin|dashboard] [Area] [Resource]
---

# Add React Page

The frontend slice. Read `.agents/rules/frontend/shared.md` plus the app file (`frontend/admin.md` /
`frontend/dashboard.md`) — the two apps **deliberately diverge**:

| | **admin** (operator) | **dashboard** (tenant) |
|---|---|---|
| Query params | PascalCase (`PageNumber`, `Search`) | camelCase (`pageNumber`, `search`) |
| `PagedResponse<T>` | import from `@/lib/api-types` | re-declare inline in the api module |
| Path constant | `const BASE = "/api/v1/..."` | inline the full path per call |
| Forms | **react-hook-form + zod** | **hand-rolled** controlled inputs (no RHF/zod) |
| List + create | separate routed pages (`list.tsx`, `create.tsx`) | one file with `<Dialog>` editors |
| Route wrapper | `<RouteGuard perms={[…]}>` | `withSuspense(<X/>)` (no permission gate) |
| Permissions | mirror in `src/lib/permissions.ts` | fetched from `GET /identity/permissions` (not JWT); nav gating via `perm`/`anyPerm` in `nav-data.ts`; no route guard — server 403 backstops |

Shared everywhere: types are **hand-written** (no codegen); `apiFetch<T>` from `@/lib/api-client`; `cn()` from `@/lib/cn`; `env.apiBase` from runtime `/config.json`; CVA `components/ui` + `components/list` primitives; Tailwind v4 CSS-first (tokens in `src/styles/globals.css`); `toast` from `sonner`; pages are **named exports**; `placeholderData: keepPreviousData` (v5).

## Step 1 — API module (`src/api/{resource}.ts`)

Hand-write the DTO/param/input types and thin `apiFetch` functions.

```ts
// admin
import { apiFetch } from "@/lib/api-client";
import type { PagedResponse } from "@/lib/api-types";
const BASE = "/api/v1/{module}/{resources}";

export type {Resource}Dto = { id: string; name: string; /* … */ };

export async function search{Resources}(p: { pageNumber?: number; search?: string } = {}) {
  const q = new URLSearchParams();
  q.set("PageNumber", String(p.pageNumber ?? 1));
  q.set("PageSize", "10");
  if (p.search?.trim()) q.set("Search", p.search.trim());
  return apiFetch<PagedResponse<{Resource}Dto>>(`${BASE}/search?${q}`);
}
export async function create{Resource}(input: Create{Resource}Input) {
  return apiFetch<{ id: string }>(BASE, { method: "POST", body: JSON.stringify(input) });
}
```

(dashboard: inline `type PagedResponse<T> = …`, inline the path, camelCase params, mutations often return `Promise<string>`.)

## Step 2 — Page (`src/pages/{area}/...tsx`, named export)

```tsx
export function {Resource}ListPage() {
  const [search, setSearch] = useState("");           // debounce → reset page to 1 on change
  const [pageNumber, setPage] = useState(1);
  const query = useQuery({
    queryKey: ["{resources}", { pageNumber, search }],   // hierarchical; params object last
    queryFn: () => search{Resources}({ pageNumber, search: search || undefined }),
    placeholderData: keepPreviousData,
  });
  // render with components/ui/* + components/list/* (admin: PageHeader/Field…; dashboard: Entity* family)
}
```

## Step 3 — Mutation (race-safe `mutate(arg)`)

Pass per-call data through `mutate(arg)`; read it from the callback `variables` — never from a closed-over render variable.

```tsx
const qc = useQueryClient();
const createMut = useMutation({
  mutationFn: (input: Create{Resource}Input) => create{Resource}(input),
  onSuccess: () => { toast.success("Created"); qc.invalidateQueries({ queryKey: ["{resources}"] }); },
  onError: (e) => toast.error(e instanceof ApiRequestError ? e.message : "Failed"),
});
// admin: const form = useForm({ resolver: zodResolver(schema) });  form.handleSubmit(v => createMut.mutate(v))
// dashboard: controlled useState fields; onSubmit(e){ e.preventDefault(); createMut.mutate(payload); }
```

If you need to track the in-flight item (e.g. a per-row busy state), use `onMutate: (arg) => setBusyId(arg)` reading the `mutate(arg)` value (pattern: `admin/src/pages/settings/sessions.tsx`).

## Step 4 — Register the route (`routes.tsx`)

```tsx
const {Resource}ListPage = lazyNamed(() => import("@/pages/{area}/list"), "{Resource}ListPage");
// admin — under AppShell.children, gated:
{ path: "{resources}", element: <RouteGuard perms={[{Module}Permissions.{Resources}.View]}><{Resource}ListPage /></RouteGuard> },
// dashboard — under AppShell.children, suspense only:
{ path: "{area}/{resources}", element: withSuspense(<{Resource}ListPage />) },
```

## Step 5 — (admin only) mirror the permission

Add the constant to `src/lib/permissions.ts` (`{Module}Permissions.{Resources}.View` = `"Permissions.{Resources}.View"`), and a `PERMISSION_CATALOG` entry if it belongs in the Role editor. See `add-permission`.

## Step 6 — Playwright test (`tests/{area}/{resource}.spec.ts`)

```ts
test.beforeEach(async ({ page }) => {
  // admin: seedAuthedSession(page, { ...TEST_USER, permissions: [...ADMIN_PERMS] }); await installAdminShellMocks(page);
  // dashboard: await seedAuthedSession(page, TEST_USER); await installShellMocks(page);
  await mockJsonResponse(page, "**/api/v1/{module}/{resources}**", paged([SAMPLE]));   // page mocks AFTER shell mocks
});
```

Use `mockProblemDetails(...)` for error states. Dashboard: scope row assertions with `.last()` / dialog scoping (lists render mobile + desktop copies → strict-mode double match).

## Step 7 — Verify

```bash
cd clients/{app} && npm run lint && npm run test:e2e
```

## Checklist

- [ ] API module: hand-written types, `apiFetch`, correct param casing per app (Pascal=admin, camel=dashboard)
- [ ] Page is a **named export**; `useQuery` key hierarchical + `placeholderData: keepPreviousData`
- [ ] Mutation passes data via `mutate(arg)`, invalidates in `onSuccess`
- [ ] Route via `lazyNamed`; admin wraps in `<RouteGuard perms>`, dashboard in `withSuspense`
- [ ] (admin) permission mirrored in `lib/permissions.ts`
- [ ] Playwright test: seed + shell mocks + page mocks; `lint` + `test:e2e` green
