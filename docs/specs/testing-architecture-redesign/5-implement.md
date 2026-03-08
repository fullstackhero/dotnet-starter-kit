# Phase 5: Implementation - Testing Architecture Redesign

The implementation phase followed a modular approach, centralizing testing infrastructure while ensuring loose coupling between layers.

## 1. Shared Infrastructure (`Shared.Tests`)
- **Container Orchestration**: Implemented `CustomWebApplicationFactory` using `Testcontainers.PostgreSql` and `Testcontainers.Redis`.
- **Job Synchronization**: Overrode `IJobService` with `InlineJobService` to ensure background provisioning tasks execute synchronously during tests.
- **Contract Handling**: Configured JSON serialization to match the API's naming conventions (e.g., `AccessToken` vs `Token`).

## 2. Integrated Fixes (Tier 0 Tenancy)
To support functional tests, critical multi-tenancy fixes from the `pr/tenancy-isolation-nomigration` branch were integrated:
- **Identity Multi-tenancy**: Re-applied `.IsMultiTenant()` to `Group`, `GroupRole`, and `UserGroup` configurations.
- **Persistence Safety**: Overrode `SaveChangesAsync` in `IdentityDbContext` with `TenantNotSetMode = Overwrite`.

## 3. Architecture Guard Updates
Refined the architecture rules to align with its modular monolith design while maintaining strict boundaries:
- **Dependency Rules**: Modified `BuildingBlocksIndependenceTests` to allow `*.Contracts` and `Identity.Contracts` dependencies, as these are intended for cross-module communication via interfaces.

## 4. Stability Improvements
- **Endpoint Discovery**: Corrected the Identity token issuance URL to `/api/v1/identity/token/issue` in test requests.
- **CI/CD Alignment**: Resolved all warnings related to compiler strictness (CA1515, CA1822) within the Test projects.
