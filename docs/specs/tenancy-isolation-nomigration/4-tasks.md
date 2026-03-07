# Implementation Tasks: tenancy-isolation-nomigration

## 1. Test Setup (Red)
- [x] Write missing unit tests for `TenantService` (BUG-1: Verify single DI registration; BUG-3/PERF-1: Integration tests for Deactivate/Exists).
- [x] Write unit test for `TenantProvisioningJob` to ensure it aborts on `CancellationToken` cancellation (BUG-4).
- [x] Write unit tests for `UserPermissionService` cache key generation targeting `{tenantId}` (CACHE-1).
- [x] Write unit test for `TenantThemeService` to assert `theme:default` is invalidated (CACHE-2).
- [x] Write integration test for `HasProcessedAsync` idempotency across tenants (EVENTING-2) - *Implemented in Generic.Tests for better isolation.*
- [x] Write integration test for `OutboxDispatcher` to ensure tenant context is restored (EVENTING-1) - *Implemented in Generic.Tests for better isolation.*
- [x] Write integration test for `AuditDbContext` model creation (AUDITING-1) - *Implemented in Auditing.Tests.*
- [ ] ~~Write unit tests/validation to ensure `UserSession`, `GroupRole`, `UserGroup`, and `PasswordHistory` entity configurations include `IsMultiTenant()` (IDENTITY-1, IDENTITY-2)~~ *(Reverted: No-migration constraint).*
- [x] Write unit tests for `LocalStorageService` to assert file paths include `{tenantId}` (STORAGE-1).
- [x] *(BUG-2 is verified by zero build warnings)*

## 2. Implementation (Green) [DONE]

### Modules.Multitenancy
- [x] `src/Modules/Multitenancy/Modules.Multitenancy/MultitenancyModule.cs`
  - Remove duplicate `ITenantService` registration (BUG-1).
  - Remove `await Task.CompletedTask` from `OnTenantResolveCompleted` (BUG-2).
- [x] `src/Modules/Multitenancy/Modules.Multitenancy/Services/TenantService.cs`
  - Fix TOCTOU race condition in `DeactivateAsync` using `_dbContext.TenantInfo.CountAsync(...)` (BUG-3).
  - Optimize `ExistsWithNameAsync` to use `_dbContext.TenantInfo.AnyAsync(...)` (PERF-1).
- [x] `src/Modules/Multitenancy/Modules.Multitenancy/Provisioning/TenantProvisioningJob.cs`
  - Thread `CancellationToken` instead of `CancellationToken.None` (BUG-4).
- [x] `src/Modules/Multitenancy/Modules.Multitenancy/Services/TenantThemeService.cs`
  - Ensure `ResetThemeAsync` invalidates `theme:default` cache entry (CACHE-2).

### Modules.Identity
- [x] `src/Modules/Identity/Modules.Identity/Services/UserPermissionService.cs`
  - Update `GetPermissionCacheKey` signature and implementation to include `tenantId`, and update call sites (GetPermissionCacheKey, InvalidatePermissionCacheAsync, GetPermissionsAsync) (CACHE-1).
- [ ] ~~`src/Modules/Identity/Modules.Identity/Data/Configurations/UserSessionConfiguration.cs`~~
  - ~~Add `builder.IsMultiTenant()` (IDENTITY-1)~~ *(Reverted: No-migration constraint).*
- [ ] ~~`src/Modules/Identity/Modules.Identity/Data/Configurations/GroupRoleConfiguration.cs`~~
  - ~~Add `builder.IsMultiTenant()` (IDENTITY-2)~~ *(Reverted: No-migration constraint).*
- [ ] ~~`src/Modules/Identity/Modules.Identity/Data/Configurations/UserGroupConfiguration.cs`~~
  - ~~Add `builder.IsMultiTenant()` (IDENTITY-2)~~ *(Reverted: No-migration constraint).*
- [ ] ~~`src/Modules/Identity/Modules.Identity/Data/Configurations/PasswordHistoryConfiguration.cs`~~
  - ~~Add `builder.IsMultiTenant()` (IDENTITY-2)~~ *(Reverted: No-migration constraint).*

### Modules.Auditing
- [x] `src/Modules/Auditing/Modules.Auditing/Persistence/AuditDbContext.cs`
  - Add `base.OnModelCreating(modelBuilder)` before `ApplyConfigurationsFromAssembly` (AUDITING-1).

### BuildingBlocks.Eventing & Storage
- [x] `src/BuildingBlocks/Eventing/Inbox/IInboxStore.cs` & `src/BuildingBlocks/Eventing/Inbox/EfCoreInboxStore.cs` & `src/BuildingBlocks/Eventing/InMemory/InMemoryEventBus.cs`
  - Add and pass `tenantId` to `HasProcessedAsync` (EVENTING-2).
- [x] `src/BuildingBlocks/Eventing/Outbox/OutboxDispatcher.cs`
  - Inject `IMultiTenantStore<AppTenantInfo>` / `IMultiTenantContextSetter` and restore tenant context (EVENTING-1).
- [x] `src/BuildingBlocks/Storage/Local/LocalStorageService.cs`
  - Inject `IMultiTenantContextAccessor<AppTenantInfo>` and add `tenantId` to physical file paths (STORAGE-1).

## 3. Verification & Polish [DONE]
- [x] Ensure all local tests pass (`dotnet test src/FSH.Framework.slnx`).
- [x] Ensure 0 build warnings.
- [x] Prepare files for commit.
