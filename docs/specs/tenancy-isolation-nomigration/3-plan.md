# Technical Plan: tenancy-isolation-nomigration

## Architecture & Design
This plan implements 13 targeted bug fixes across the Multitenancy, Identity, Eventing, Auditing, and Storage modules to enforce strict tenant isolation. The fixes are designed to require zero database schema changes (no EF Core migrations). The modifications respect the Modular Monolith boundaries, ensuring that changes within the isolated modules do not leak, while carefully updating the protected `BuildingBlocks` to support tenant-aware event idempotency and file storage paths.

## Proposed Changes (File Level)

### Modules.Multitenancy
- `src/Modules/Multitenancy/Modules.Multitenancy/MultitenancyModule.cs`: Remove duplicate `ITenantService` registration and unnecessary `await Task.CompletedTask`.
- `src/Modules/Multitenancy/Modules.Multitenancy/Services/TenantService.cs`: Fix race condition in `DeactivateAsync` via atomic database count check. Optimize `ExistsWithNameAsync` to use `AnyAsync`.
- `src/Modules/Multitenancy/Modules.Multitenancy/Provisioning/TenantProvisioningJob.cs`: Thread `CancellationToken` instead of hardcoding `CancellationToken.None`.
- `src/Modules/Multitenancy/Modules.Multitenancy/Services/TenantThemeService.cs`: Ensure `ResetThemeAsync` properly invalidates the `theme:default` cache entry.

### Modules.Identity
- `src/Modules/Identity/Modules.Identity/Services/UserPermissionService.cs`: Scope caching keys using `tenantId` instead of just `userId`.
- `src/Modules/Identity/Modules.Identity/Data/Configurations/UserSessionConfiguration.cs`: Enforce tenant isolation via `.IsMultiTenant()`.
- `src/Modules/Identity/Modules.Identity/Data/Configurations/GroupRoleConfiguration.cs`: Enforce tenant isolation via `.IsMultiTenant()`.
- `src/Modules/Identity/Modules.Identity/Data/Configurations/UserGroupConfiguration.cs`: Enforce tenant isolation via `.IsMultiTenant()`.
- `src/Modules/Identity/Modules.Identity/Data/Configurations/PasswordHistoryConfiguration.cs`: Enforce tenant isolation via `.IsMultiTenant()`.

### Modules.Auditing
- `src/Modules/Auditing/Modules.Auditing/Persistence/AuditDbContext.cs`: Re-introduce soft-delete global query filters by calling `base.OnModelCreating(modelBuilder)`.

### BuildingBlocks.Eventing (Approved Modifications)
- `src/BuildingBlocks/Eventing/Inbox/IInboxStore.cs`: Adjust `HasProcessedAsync` signature to include `tenantId`.
- `src/BuildingBlocks/Eventing/Inbox/EfCoreInboxStore.cs`: Implement the signature adjustment, querying with `TenantId`.
- `src/BuildingBlocks/Eventing/Outbox/OutboxDispatcher.cs`: Inject tenant context dependencies to restore the originating tenant context prior to dispatching queued events.
- `src/BuildingBlocks/Eventing/InMemory/InMemoryEventBus.cs`: Accommodate the new `tenantId` argument when calling `HasProcessedAsync`.

### BuildingBlocks.Storage (Approved Modifications)
- `src/BuildingBlocks/Storage/Local/LocalStorageService.cs`: Inject tenant context dependency and restructure physical file path logic to prepend `tenantId/`.

## Testing Strategy
- **Integration Specs (`Spec.Tests`)**: 
  - `HasProcessedAsync` idempotency handles identical payloads across different tenants correctly (EVENTING-2).
  - Soft-deleted `AuditRecord`s do not appear in queries (AUDITING-1).
  - Outbox dispatch correctly restores the original `TenantId` context when publishing (EVENTING-1).
  - `DeactivateAsync` and `ExistsWithNameAsync` interact atomically/efficiently with the database (BUG-3, PERF-1).
- **Unit Tests (`{Module}.Tests` & `Architecture.Tests`)**: 
  - `IServiceCollection` contains exactly one registration for `ITenantService` (BUG-1).
  - `TenantProvisioningJob` correctly honors a cancelled `CancellationToken` (BUG-4).
  - `UserPermissionService.GetPermissionCacheKey` guarantees the cache key incorporates `{tenantId}` (CACHE-1).
  - `TenantThemeService.ResetThemeAsync` explicitly invalidates the `theme:default` cache entry (CACHE-2).
  - Validate the `IsMultiTenant()` configuration properties for `UserSession`, `GroupRole`, `UserGroup`, and `PasswordHistory` (IDENTITY-1, IDENTITY-2).
  - `LocalStorageService` physical path resolving correctly includes the `{tenantId}` (STORAGE-1).
  *Note: BUG-2 (`await Task.CompletedTask`) is purely a compiler warning/syntactic fix, which will be verified by the build succeeding with 0 warnings.*
