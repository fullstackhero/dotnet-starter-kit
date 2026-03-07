# Specification: Testing Architecture Redesign

## 1. Description
The current test suite has yielded false positives, masking API failures regarding Authentication, Middlewares, and Database Migrations. The use of `Microsoft.EntityFrameworkCore.InMemory` bypasses critical relational DB checks and ignores ASP.NET Core pipelines. We need to redesign the testing architecture to logically separate concerns into pure Unit tests, Integration tests, Functional tests, and Acceptance/Spec tests, powered by Docker-based `Testcontainers`.

## 2. Requirements & User Stories
- **Requirement 1**: Prevent false positives by using a real ephemeral database (via Testcontainers) instead of `InMemoryDatabase` for integration and functional tests.
- **Requirement 2**: Centralize the shared test infrastructure (Testcontainers, Respawn, WebApplicationFactory) into a new `Tests.Shared` core project to prevent code duplication.
- **Requirement 3**: Isolate tests that only verify Mediator handlers and DB constraints (no HTTP layer) into a new `Integration.Tests` project.
- **Requirement 4**: Isolate vertical slice tests (HTTP requests down to the DB) into a new `Functional.Tests` project.
- **Requirement 5**: Evolve the existing `Spec.Tests` to inherit from the functional testing infrastructure, allowing true BDD/Acceptance testing against a real HTTP and DB environment.
- **Requirement 6**: Progressively clean up the existing `*.Tests` projects to remove `InMemoryDatabase` in favor of pure mock-based unit tests (`NSubstitute`).

## 3. Acceptance Criteria
- [ ] `Directory.Packages.props` updated with `Testcontainers`, `Respawn`, and `Microsoft.AspNetCore.Mvc.Testing`.
- [ ] `Tests.Shared` project created and handles Testcontainer Orchestration.
- [ ] `Playground.Api/Program.cs` is made visible.
- [ ] `Functional.Tests` project created, configured to use `Tests.Shared`, and containing at least 1 working HTTP Login test.
- [ ] `Integration.Tests` project created, configured to use `Tests.Shared` (direct Mediator testing).
- [ ] `Spec.Tests` refactored/configured to utilize the new infrastructure for acceptance tests.
