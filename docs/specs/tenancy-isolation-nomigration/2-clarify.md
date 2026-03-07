# Clarifications: tenancy-isolation-nomigration

## Unresolved Questions

1. **BuildingBlocks Protection**: Per the `.agents/rules/buildingblocks-protection.md` rule, the `src/BuildingBlocks/` packages are heavily protected and should rarely be modified without explicit architectural approval. The specification for this issue requires modifying the following BuildingBlocks files:
   - `src/BuildingBlocks/Eventing/Inbox/IInboxStore.cs`: Adding `string? tenantId` to `HasProcessedAsync` (Breaking change for any custom implementations).
   - `src/BuildingBlocks/Eventing/Inbox/EfCoreInboxStore.cs`: Implementing the above interface change.
   - `src/BuildingBlocks/Eventing/InMemory/InMemoryEventBus.cs`: Passing `@event.TenantId` to `HasProcessedAsync`.
   - `src/BuildingBlocks/Eventing/Outbox/OutboxDispatcher.cs`: Injecting `IMultiTenantStore<AppTenantInfo>` and `IMultiTenantContextSetter` to restore the tenant context before publishing.
   - `src/BuildingBlocks/Storage/Local/LocalStorageService.cs`: Injecting `IMultiTenantContextAccessor<AppTenantInfo>` to isolate the physical file storage paths per tenant.
   
   **Question**: Do I have your explicit approval to modify these protected BuildingBlocks files to implement the fixes?

## Decisions Made
The user has granted explicit approval to modify the required `BuildingBlocks` files because solving these 13 critical tenancy bugs takes precedence. The modifications will strictly follow the solutions outlined in GitHub Issue #6.
