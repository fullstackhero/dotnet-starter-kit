# Technical Plan: Testing Architecture Redesign

## Architecture & Design
To cleanly segregate testing responsibilities and prevent false positives, we will reorganize the test projects into a star hierarchy centered around a new shared infrastructure project. We will transition from non-relational `InMemoryDatabase` testing to actual relational environment testing using `Testcontainers`.

**Hierarchy:**
- `Tests.Shared` (Base Infrastructure)
- `Integration.Tests` (References `Tests.Shared`)
- `Functional.Tests` (References `Tests.Shared` + `Playground.Api`)
- `Spec.Tests` (References `Functional.Tests`)
- `*.Tests` (Unit Tests)

## Proposed Changes (File Level)

### Directory.Packages.props
- `src/Directory.Packages.props`: Add global `<PackageVersion>` references for `Testcontainers.PostgreSql`, `Testcontainers.Redis`, `Microsoft.AspNetCore.Mvc.Testing`, and `Respawn`.

### Solution Structure
- `src/FSH.Framework.slnx`: Add references for the new `Shared.Tests`, `Integration.Tests`, and `Functional.Tests` projects.

### Core API
- `src/Playground/Playground.Api/Program.cs`: Add `public partial class Program { }` at the end of the file to allow visibility for the `WebApplicationFactory`.

### `Tests.Shared` (New Core Component)
- `src/Tests/Shared.Tests/Shared.Tests.csproj`: New xUnit project.
- `src/Tests/Shared.Tests/Infrastructure/CustomWebApplicationFactory.cs`: Overrides the host builder to spin up PostgreSQL and Redis Testcontainers and injects their connection strings into the test configuration.

### `Functional.Tests` (New Component)
- `src/Tests/Functional.Tests/Functional.Tests.csproj`: New xUnit project referencing `Shared.Tests` and `Playground.Api`.
- `src/Tests/Functional.Tests/Infrastructure/BaseFunctionalTest.cs`: Base class implementing `IClassFixture<CustomWebApplicationFactory>`, exposing an authenticated `HttpClient`.
- `src/Tests/Functional.Tests/Identity/TokenEndpointTests.cs`: A functional test ensuring the `/api/v1/tokens` endpoint works end-to-end with the real database.

### `Integration.Tests` (New Component)
- `src/Tests/Integration.Tests/Integration.Tests.csproj`: New xUnit project referencing `Shared.Tests` and `Core`.
- `src/Tests/Integration.Tests/Infrastructure/BaseIntegrationTest.cs`: Base class exposing `ISender` (Mediator) and `DbContext` for direct command testing (sidestepping HTTP).

## Testing Strategy
- **Integration Specs (`Spec.Tests`)**: Will be configured to inherit from `BaseFunctionalTest` to use the Testcontainers infrastructure for BDD scenarios.
- **Unit Tests (`*.Tests`)**: No immediate changes required to existing tests other than laying the groundwork for future cleanup of `InMemoryDatabase` usage.
- **Functional Tests (`Functional.Tests`)**: We will implement 1 core Critical Path test (Login/Tokens) to prove the new `WebApplicationFactory` architecture functions correctly.
