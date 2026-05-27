---
name: add-full-slice
description: Build a capability end-to-end — backend vertical slice (Contracts→handler→validator→endpoint) AND the React page wired to it. Use when delivering a user-facing feature across API + UI. Composes add-feature + add-react-page.
argument-hint: [ModuleName] [admin|dashboard] [FeatureName]
---

# Add Full Slice (backend → frontend)

The kit's accelerator: deliver a feature from database to UI in one pass. This skill **composes**
`add-feature` (backend) and `add-react-page` (frontend) — follow each for the detailed code; this file is
the order of operations and the **contract** that keeps the two halves in sync.

## Order of operations

1. **Backend slice** — `add-feature` (and `add-entity` first if a new entity is needed):
   - Command/Query + response DTO in `Modules.{X}.Contracts/v1/{Area}/` (+ `Contracts/Dtos/`).
   - Handler (`public sealed`, injects `{X}DbContext`), Validator, Endpoint (`internal static Map…Endpoint`, `.RequirePermission(...)`).
   - Wire in `{X}Module.MapEndpoints`. Build + test backend green.
2. **Lock the contract** — note the final **route path**, HTTP method, request shape, and response DTO field names/casing. The React side must match these exactly.
3. **Frontend page** — `add-react-page` in the chosen app:
   - API module calls the **same path**; hand-write TS types mirroring the **response DTO** (the API serializes C# records as camelCase JSON — TS fields are camelCase even though admin *query params* are PascalCase).
   - Page (`useQuery`/`useMutation`), route (`RouteGuard` for admin / `withSuspense` for dashboard).
4. **Permission** — if the endpoint is gated, mirror the constant into admin's `lib/permissions.ts` and gate the route (`add-permission`). Dashboard relies on the server 403.
5. **Tests both sides** — backend handler/validator test (xUnit/Shouldly/NSubstitute) + frontend Playwright spec (route-mocked).

## The contract (the thing that breaks if you're sloppy)

| Backend | Frontend must match |
|---|---|
| Endpoint route `api/v{n}/{module}/{resources}` | `apiFetch` path |
| Request: the `Command`/`Query` record fields | request body / query-param keys (admin params PascalCase; **body JSON camelCase**) |
| Response: the DTO record (camelCase JSON) | the hand-written TS `type` |
| `.RequirePermission({X}Permissions...)` | (admin) `RouteGuard perms` + the mirrored constant |
| Paginated → `PagedResponse<T>` | `PagedResponse<T>` (admin: `@/lib/api-types`; dashboard: inline) |

## Verify end-to-end

```bash
dotnet build src/FSH.Starter.slnx && dotnet test src/Tests/{X}.Tests
cd clients/{app} && npm run lint && npm run test:e2e
# optional manual check: dotnet run --project src/Host/FSH.Starter.AppHost  (brings up API + both apps)
```

## Checklist

- [ ] Backend slice complete + green (`add-feature`)
- [ ] Contract locked: route, request shape, response DTO field names
- [ ] Frontend api module path + TS types match the contract (body JSON camelCase)
- [ ] Page + route added (`add-react-page`); admin permission mirrored + gated (`add-permission`)
- [ ] Backend test + Playwright test added; both suites green
