# Phase 6: Walkthrough - Testing Architecture Redesign

## Overview
The testing architecture for the .NET Starter Kit has been successfully redesigned and implemented. We transitioned away from the fragile `Microsoft.EntityFrameworkCore.InMemory` implementation and adopted a robust, containerized approach using **Testcontainers**. The integration relies on ephemeral PostgreSQL and Redis instances that emulate the exact production environment for our tests.

## Architecture Highlights
The testing ecosystem is now structured as follows:

1. **`Tests.Shared` (Core Infrastructure)**
   - Houses `CustomWebApplicationFactory`, which orchestrates the Testcontainers (PostgreSQL 16 and Redis 7) lifecycle.
   - Replaces the application's `IJobService` with an `InlineJobService` during the test execution, guaranteeing that traditionally asynchronous Hangfire jobs (like Tenant Database Seeding) execute synchronously. This definitively eliminates flaky `401 Unauthorized` test outcomes.
   - Provides reusable cleanup utilities and the baseline DI configurations.

2. **`Integration.Tests` (Domain & Infrastructure)**
   - Inherits from `BaseIntegrationTest`.
   - Bypasses the HTTP stack entirely. Uses `ISender` (Mediator) to execute Commands and Queries directly against the real ephemeral database.
   - **Verification:** `Tenant_ShouldBeRetrieved_WhenExistsInDatabase_ViaMediator` passed successfully.

3. **`Functional.Tests` (Vertical Slices)**
   - Inherits from `BaseFunctionalTest`.
   - Leverages `HttpClient` connected to the Testcontainers server to hit the actual API endpoints, effectively traversing the Middlewares, Routing, Authorization, and database layers.
   - **Verification:** `Identity_Login_ShouldReturnValidToken_WhenCredentialsAreCorrect` successfully logs in the seeded admin user and retrieves a valid JWT.

4. **`Spec.Tests` (Acceptance & Behavior)**
   - Retained the existing BDD/Acceptance layer, but it now seamlessly inherits from the `Functional.Tests` infrastructure.
   - **Verification:** `SetupSanityCheckTests` passed successfully within the new containerized constraints.

## Technical Details & Enhancements
- **Multi-Tenancy Integration**: Synchronized critical fixes from Issue #6, ensuring that custom Identity entities (Groups) are correctly isolated and seeded without `NullConstraint` violations.
- **Architecture Test Refinement**: Updated dependency rules to distinguish between "hidden" implementation details and "exposed" Contracts, allowing legitimate cross-module interaction via interfaces.
- **Solution File (`FSH.Framework.slnx`):** Updated to include the three new testing projects.
- **Dependency Management:** Migrated testing dependencies (like `Testcontainers`, `Respawn`) explicitly to `Directory.Packages.props`.
- **Zero-Warnings Policy & Code Analysis:** Addressed all ASP.NET strict analysis warnings (`CA1062`, `CA1822`, `CA1051`, `CS8714`, `CA1515`, `CS8604`, `CA2016`, `CA1849`), SonarQube issues (`S6667`, `S1135`, `S108`, `S1118`, `S2930`, `S6966`), and centrally upgraded `MimeKit`/`MailKit` to `4.15.1` to resolve `NU1902/NU1603` vulnerabilities, ensuring the repository aligns seamlessly with CI/CD compilation standards.

## Handling Container Concurrency
During the transition to Testcontainers, three key concurrency challenges were mitigated to ensure clean tests:
1. **Startup Race Conditions**: Background services (like `OutboxDispatcherHostedService`) often start polling before EF Core has finished creating the database tables. This was fixed by implementing graceful degradation—catching `System.Data.Common.DbException` (`42P01`) and returning an empty list, allowing the application to wait cleanly until tables exist. Additionally, `IHostApplicationLifetime.ApplicationStarted` is now awaited before the polling loop begins.
2. **EF Core "First Run" Logs**: When connecting to a completely empty PostgreSQL container, EF Core logs a `Failed executing DbCommand` `ERR` as it probes for `__EFMigrationsHistory`. This is a standard and safe internal EF Core behavior that creates the table if missing, and should be safely ignored in test outputs.
3. **Teardown Cancellations**: Shutting down `WebApplicationFactory` abruptly cancels background channels. `OperationCanceledException` is now caught silently in `AuditBackgroundWorker` to prevent spurious teardown logs.

## Verification Results

### Architecture Tests (47/47 Passed)
```bash
Passed!  - Failed:     0, Passed:    47, Skipped:     0, Total:    47, Duration: 1 s
```

### Functional Tests (Successful Login)
```bash
Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: 8 s
```

- **Deprecating InMemory:** Teams should progressively remove `InMemoryDatabase` from `Identity.Tests` and other legacy unit test boundaries.
- **Writing New Tests:** Developers must now follow the standard base classes (`BaseFunctionalTest` or `BaseIntegrationTest`) when adding new features through `.agents/skills`.

### PostgreSQL Shutdown Warning Fix
- **Issue:** `Npgsql.NpgsqlException` was occasionally thrown during test suite teardown.
- **Cause:** The database container was being disposed of before the `WebApplicationFactory` host had completed its graceful shutdown process.
- **Fix:** Implemented a custom `DisposeAsync` in `CustomWebApplicationFactory` that explicitly calls `base.DisposeAsync()` first. This ensures all background services (like Hangfire) and the application host finish their work while the PostgreSQL container is still operational.

_This completes Issue #23._
