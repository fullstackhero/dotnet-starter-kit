# Specification: tenancy-isolation-nomigration (13 Critical Tenancy Fixes)

## 1. Description
This specification addresses 13 critical bugs in the Multitenancy implementation that compromise tenant isolation, data consistency, and application performance. These bugs span across various modules including Multitenancy, Identity, Eventing, Auditing, and Storage. Crucially, all 13 fixes can and must be implemented without requiring any database migrations.

## 2. Requirements & User Stories
- **BUG-1 (Duplicate ITenantService)**: Remove the duplicate registration of `ITenantService` in `MultitenancyModule.cs`.
- **BUG-2 (Superfluous await)**: Remove the unnecessary `await Task.CompletedTask` in the `OnTenantResolveCompleted` event handler.
- **BUG-3 (TOCTOU in DeactivateAsync)**: Fix the race condition in `TenantService.DeactivateAsync` by using an atomic database check for active tenants instead of in-memory lists.
- **BUG-4 (CancellationToken.None)**: Replace hardcoded `CancellationToken.None` in `TenantProvisioningJob.RunAsync` with properly forwarded cancellation tokens.
- **CACHE-1 (Permission cache key)**: Update `UserPermissionService.GetPermissionCacheKey` to include `tenantId`, preventing cross-tenant cache collisions for users with identical IDs.
- **CACHE-2 (Default Theme Cache)**: Ensure `TenantThemeService.ResetThemeAsync` invalidates the `theme:default` cache entry in addition to the tenant-specific theme when resetting the default tenant's theme.
- **EVENTING-2 (HasProcessedAsync lacks TenantId)**: Add a `TenantId` filter to `EfCoreInboxStore.HasProcessedAsync` to fix cross-tenant idempotency issues.
- **EVENTING-1 (OutboxDispatcher Tenant Context)**: Restore the proper tenant context in `OutboxDispatcher` before publishing each outbox message.
- **AUDITING-1 (Soft-delete filter bypass)**: Call `base.OnModelCreating(modelBuilder)` in `AuditDbContext` before applying configurations to ensure global query filters (like soft delete) are applied to audit records.
- **IDENTITY-1 (UserSession Tenant Isolation)**: Add `.IsMultiTenant()` to `UserSessionConfiguration` to properly isolate user sessions per tenant.
- **IDENTITY-2 (Identity Entities Tenant Isolation)**: Add `.IsMultiTenant()` to `GroupRoleConfiguration`, `UserGroupConfiguration`, and `PasswordHistoryConfiguration` to prevent cross-tenant data leaks.
- **PERF-1 (ExistsWithNameAsync memory exhaustion)**: Refactor `TenantService.ExistsWithNameAsync` to use `AnyAsync` directly on the database context instead of loading all tenants into memory via `GetAllAsync()`.
- **STORAGE-1 (LocalStorage path lacks tenantId)**: Update `LocalStorageService` to include the `tenantId` in the physical file path, isolating uploaded files per tenant and preventing direct URL guessing attacks.

## 3. Acceptance Criteria
- [ ] `MultitenancyModule.cs` has exactly one `AddScoped<ITenantService, TenantService>()` call.
- [ ] `OnTenantResolveCompleted` lambda no longer has `await Task.CompletedTask`.
- [ ] `DeactivateAsync` count check uses `_dbContext.TenantInfo.CountAsync(...)` not `GetAllAsync()`.
- [ ] All `CancellationToken.None` in `TenantProvisioningJob` replaced with forwarded token.
- [ ] `GetPermissionCacheKey` returns `perm:{tenantId}:{userId}`.
- [ ] `ResetThemeAsync` invalidates both `theme:{tenantId}` and `theme:default`.
- [ ] `HasProcessedAsync` filters on `TenantId`.
- [ ] `OutboxDispatcher` sets tenant context before each message dispatch.
- [ ] `AuditDbContext.OnModelCreating` calls `base.OnModelCreating(modelBuilder)` before `ApplyConfigurationsFromAssembly`.
- [ ] `UserSessionConfiguration`, `GroupRoleConfiguration`, `UserGroupConfiguration`, `PasswordHistoryConfiguration` all call `builder.IsMultiTenant()`.
- [ ] `ExistsWithNameAsync` uses `AnyAsync` on `_dbContext.TenantInfo`.
- [ ] Local storage upload path includes `{tenantId}` segment.
