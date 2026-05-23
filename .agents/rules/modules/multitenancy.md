# Module: Multitenancy

Tenant catalog, provisioning, activation/upgrade, per-tenant theming (Finbuckle.MultiTenant). Foundational — registered early.

**Entities / DbContext:** `AppTenantInfo` (catalog), `TenantProvisioning` + `TenantProvisioningStep`, `TenantTheme`. `TenantDbContext` holds the tenant catalog in the main DB.
**Areas:** CreateTenant, ChangeTenantActivation, UpgradeTenant, Get(Tenants/Status/Migrations), TenantProvisioning (status/retry), TenantTheme (get/update/reset). Full list: `Features/v1/` or `/scalar`.

## Gotchas

- **Finbuckle pipeline ordering** — strategy chain Claim → Header → `?tenant=` → DistributedCache → EFCoreStore, but `UseMultiTenant()` runs **before `UseAuthentication()`**, so the claim strategy no-ops (User is anonymous at resolution time). Resolution is effectively **header-driven** (`MultitenancyConstants.Identifier`).
- **Root-operator cross-tenant override is a post-auth middleware** in `ConfigureMiddleware` (not a Finbuckle strategy). Gate: caller's JWT tenant claim == `MultitenancyConstants.Root.Id` **and** a `tenant` header != root; it re-resolves via `IMultiTenantContextSetter`. Claim-aware tenant logic must go here, never in a strategy.
- **`ITenantInitialPasswordBuffer`** (singleton) — the tenant admin password is **operator-supplied**, not a constant. `CreateTenantCommandHandler` calls `Store(tenantId, password)` **before** kicking off provisioning; the background seed step `TryConsume`s it (`ConcurrentDictionary`, consume = remove).
- **Provisioning** runs 4 steps (Database → Migrations → Seeding → CacheWarm) via a Hangfire `TenantProvisioningJob`, falling back to inline execution if Hangfire storage is unavailable. **Activation is gated on `Status == Completed`.**
- `ITenantService.MigrateTenantAsync`/`SeedTenantAsync` create a fresh scope and set `IMultiTenantContext` **first**, then run the `IDbInitializer`s.

Tenant **isolation** mechanics (default-on filter, `IGlobalEntity` opt-out, `base.OnModelCreating` last) live in `database.md`.
