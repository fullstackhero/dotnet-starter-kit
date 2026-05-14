# Issue Triage — 2026-05-14

Audit of every open issue against the current `develop` branch (post-.NET-10 rewrite, React + Vite frontend, modular monolith).

## Summary

| Outcome | Count | Issues |
|---|---|---|
| **Close — superseded by rewrite** | 12 | #1084, #1090, #1104, #1109, #1115, #1120, #1121, #1129, #1138, #1143, #1210 (+ #1118) |
| **Close — already fixed in current code** | 2 | #1136, #1230 |
| **Close — fix applied in this pass** | 1 | #1205 (README link) |
| **Close — answered / by design / out of scope** | 3 | #1116, #1117, #1130 |
| **Keep open** | 5 | #1149, #1157, #1173, #1178, #1237 |
| **Total** | 23 | |

Reference point for "the rewrite": Mukesh confirmed on #1157 (2025-12-28) that the repo was rewritten on .NET 10 with new paths (`src/BuildingBlocks`, `src/Modules`, `src/Host`) and a React + Vite frontend (admin + dashboard). Anything filed before that against `src/api/framework/...`, `src/aspire/host/Host.csproj`, MudBlazor, `JwtAuthenticationService.cs`, etc. is against code that no longer exists.

## Per-issue decisions

### Close — superseded by .NET 10 rewrite

#### #1084 — Incompatible Blazor Web App and Blazor Server (2025-01-21)
Against the .NET 7 Blazor WASM frontend. The Blazor frontend was removed entirely; the dashboard is now React + Vite. The specific code (`JwtAuthenticationHeaderHandler`, `JwtAuthenticationService.cs`) no longer exists.

#### #1090 — Responsive UI (2025-02-02)
Screenshot is from the old MudBlazor UI. That UI shipped from the repo. Spam attachment in the only comment.

#### #1104 — MudTable example (2025-03-12)
Asks for a MudBlazor table example. MudBlazor is no longer a dependency.

#### #1109 — Bump to Ardalis Specification 9.0.1 (2025-04-01)
Repo no longer depends on `Ardalis.Specification`. The new framework defines its own `Specification<T>` at `src/BuildingBlocks/Persistence/Specifications/Specification.cs` (with `SpecificationEvaluator` and `SpecificationExtensions`). The `OrderedSpecificationBuilder` extension methods called out in the issue do not exist in the new codebase.

#### #1115 — How to integrate xUnit test project (2025-04-10)
Tests are now first-class: `src/Tests/Architecture.Tests`, `Auditing.Tests`, `Caching.Tests`, `Catalog.Tests`-area coverage, `Chat.Tests`, `Files.Tests`, `Generic.Tests`, `Identity.Tests`, `Integration.Tests`, `Multitenancy.Tests`. CLAUDE.md documents conventions (xUnit, Shouldly, NSubstitute, AutoFixture, NetArchTest).

#### #1120 — Cannot open project file `/src/aspire/host/Host.csproj` (2025-04-30)
Path doesn't exist. Aspire host moved to `src/Host/FSH.Starter.AppHost/FSH.Starter.AppHost.csproj`.

#### #1121 — Database migrations for CatalogDbContext (2025-05-07)
Filed against pre-rewrite Catalog module. Current Catalog module (`src/Modules/Catalog/Modules.Catalog/Data/CatalogDbContext.cs`) is in a different shape; migrations are applied via the dedicated `FSH.Starter.DbMigrator` console project (see commit `38560bf7`). The design-time `IMultiTenantContextAccessor` resolution issue from the old code path is not present in the new flow.

#### #1129 — Few Identity Endpoints (2025-06-11)
Pre-rewrite. The current `Modules.Identity` ships full surface for Users, Roles, Groups, Sessions, Tokens — far beyond what was missing in 2025-06. No actionable list in the issue.

#### #1138 — Prevents "database does not exist" (2025-08-13)
Code-dump issue proposing a hosted service in `src/api/framework/Infrastructure/Persistence/Extensions.cs`. That path no longer exists. Database/schema creation is now handled by Aspire (`AddPostgres("postgres").AddDatabase("fsh-db")`) plus the `FSH.Starter.DbMigrator` console project, which the AppHost waits for via `WaitForCompletion` before starting the API. The reliability problem this issue tried to patch is solved structurally.

#### #1143 — Error accessing the register page (2025-09-17)
User's own Blazor `SelfRegister.razor` with a custom `GetSchoolsListAsync` — that's not in FSH. Filed against an older Blazor branch (user later confirmed: "version before the vertical slice version"). The frontend is no longer Blazor in the current repo.

#### #1210 — `_currentUserId` gets incorrect value (2026-02-28)
Code sample uses `AuthenticationStateProvider` and Razor pages — the current repo has zero `.razor` files. Issue applies to the old Blazor frontend that has been removed.

#### #1118 — Blazor Server plans (2025-04-21)
Frontend is now React + Vite. No plan to add Blazor Server.

### Close — already fixed in current code

#### #1136 — IdentityDbContext caused StackOverflow in SaveChanges interceptor (2025-07-28)
Architectural fix already in place:
- The audit interceptor is now `AuditingSaveChangesInterceptor` (`src/Modules/Auditing/Modules.Auditing/Persistence/AuditingSaveChangesInterceptor.cs`).
- It has an explicit recursion guard at line 33: `if (ctx is AuditDbContext) return result;`.
- Audit writes go to a separate `AuditDbContext`, not back into `IdentityDbContext`.
- The interceptor is async-friendly and uses `IAuditPublisher` via a channel — the synchronous re-entrant `SaveChanges` chain from the old design is gone.

#### #1230 — AuditInterceptor feedback loop (latent) (2026-03-27)
The exact mitigation the issue proposed is now in place — see the recursion guard at `AuditingSaveChangesInterceptor.cs:33`:

```csharp
// Never audit the audit store's own writes — it would recurse: each flush would
// capture the AuditRecord inserts, whose PayloadJson embeds the prior PayloadJson,
// growing exponentially until System.Text.Json rejects the payload.
if (ctx is AuditDbContext) return result;
```

Even better than the issue's proposal: the guard is unconditional (doesn't depend on the `IAuditable` filter), so it survives the two failure modes the reporter identified (audit entity gaining `IAuditable`, or broadening the entry filter).

### Close — fix applied in this pass

#### #1205 — Docs missing (`docs/framework/architecture.md`) (2026-02-03)
The README pointed to `docs/framework/architecture.md` and `docs/framework/developer-cookbook.md`, which never moved when the docs were rewritten into the Astro Starlight site at `docs/src/content/docs/`. **Fixed in this triage pass**: README.md now points to the new docs location (`docs/src/content/docs/architecture.mdx` and the broader docs site).

### Close — answered / by design / out of scope

#### #1116 — Angular starter template (2025-04-11)
Out of scope. The frontend stack is React + Vite (admin + dashboard). No Angular template planned.

#### #1117 — Encapsulate API return values into a unified format (2025-04-14)
Already addressed by design — the API follows RFC 9457 ProblemDetails for errors and returns native typed payloads for successes. CLAUDE.md explicitly documents this: "Global handler converts to `ProblemDetails` (RFC 9457)". Wrapping every response in `{code, data, message}` would break OpenAPI typing, Scalar docs, and the typed React clients generated from the OpenAPI spec. By-design close.

#### #1130 — Aspire MSSQL Database (2025-06-28)
Answered in the comment thread (`AddSqlServer().AddDatabase(...)`). The current `AppHost.cs` shows the pattern for Postgres; swapping to SQL Server is one line. The deeper "SQL Server is a first-class option in Aspire" gap is tracked by **#1149**.

### Keep open

#### #1149 — jsonb for SQL Server + Aspire conditional SQL startup (2025-11-19)
**Valid gap.** The repo claims "Postgres by default, SQL Server ready" but:
- `AppHost.cs` hardcodes `AddPostgres("postgres")` with no conditional branch.
- Only `FSH.Starter.Migrations.PostgreSQL` exists; no `FSH.Starter.Migrations.MSSQL` project.
- Several entity configurations use `jsonb` (Postgres-only) directly: `AuditRecordConfiguration.cs:18`, `MetadataJson`/`PayloadJson`/`OverageRates` columns in Notifications, Billing, Multitenancy migrations.

To deliver "SQL Server ready" honestly we need: provider-aware column type mapping (`jsonb` ↔ `nvarchar(max)`), a sibling MSSQL migrations project, and Aspire branching driven by config. Keep open as a feature gap.

#### #1157 — ASP.NET Core OData v8 with Swagger/OpenAPI (2025-12-27)
Feature request, applies cleanly to the current .NET 10 codebase (Mukesh confirmed in-thread). Keep open. Decision point: OData vs the existing `Specification<T>` pattern — they overlap conceptually, so the design needs a call on whether OData is exposed as an additional read surface or replaces specifications for some endpoints.

#### #1173 — Improve handler test coverage (2026-01-25)
Mukesh's own tracking issue. Progress noted: 5/60+ handlers tested via PR #1204. Keep open; still active.

#### #1178 — Extract hardcoded error strings to resource files (2026-01-25)
Mukesh's own i18n issue. Still valid against current code (no `IStringLocalizer` usage detected for error messages). Keep open.

#### #1237 — Settings Module (2026-05-09)
Feature request; Mukesh confirmed it's planned ("Currently done with the Files Module, and working on the Notifications / Chat modules"). Keep open as a tracked feature.

## Actions taken this pass

1. README.md updated to fix the broken `docs/framework/...` references (resolves #1205).
2. 18 issues closed with detailed per-issue rationale (this file is referenced from each closing comment).
3. Triage record committed to the repo at `docs/ISSUE_TRIAGE_2026-05-14.md`.
