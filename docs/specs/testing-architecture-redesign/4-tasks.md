# Implementation Tasks: Testing Architecture Redesign

## 1. Test Setup (Red Phase)
*Write failing tests first to define the success criteria for the new infrastructure.*
- [x] **Functional:** Write `Identity_Login_ShouldReturnValidToken_WhenCredentialsAreCorrect` in `Functional.Tests`.
- [x] **Integration:** Write `Tenant_ShouldBeRetrieved_WhenExistsInDatabase_ViaMediator` in `Integration.Tests`.
- [x] **Spec:** Refactor `SetupSanityCheckTests.cs` in `Spec.Tests` to inherit from the new shared infrastructure.

## 2. Implementation (Green)
### 2.1 Shared Infrastructure
- [x] Add `Testcontainers`, `Testcontainers.PostgreSql`, `Testcontainers.Redis`, `Respawn`, and `Microsoft.AspNetCore.Mvc.Testing` to `Directory.Packages.props`.
- [x] Add `public partial class Program { }` to `src/Playground/Playground.Api/Program.cs`.
- [x] Create project `src/Tests/Shared.Tests/Shared.Tests.csproj`.
- [x] Implement `CustomWebApplicationFactory.cs` (orchestrating Docker containers) in `Shared.Tests`.
- [x] Add `Shared.Tests` to `FSH.Framework.slnx`.

### 2.2 Functional Layer
- [x] Create project `src/Tests/Functional.Tests/Functional.Tests.csproj` referencing `Shared.Tests` and `Playground.Api`.
- [x] Implement `BaseFunctionalTest.cs` (handling `HttpClient` and JWT Token generation) in `Functional.Tests`.
- [x] Execute `Identity_Login_ShouldReturnValidToken_WhenCredentialsAreCorrect` and ensure it passes (Green).
- [x] Add `Functional.Tests` to `FSH.Framework.slnx`.

### 2.3 Integration Layer
- [x] Create project `src/Tests/Integration.Tests/Integration.Tests.csproj` referencing `Shared.Tests`.
- [x] Implement `BaseIntegrationTest.cs` (exposing `ISender` without HTTP) in `Integration.Tests`.
- [x] Add `Integration.Tests` to `FSH.Framework.slnx`.

### 2.4 Spec Layer Alignment
- [x] Add `<ProjectReference>` to `Functional.Tests` inside `src/Tests/Spec.Tests/Spec.Tests.csproj`.

## 3. Verification & Polish
- [x] Run `dotnet build src/FSH.Framework.slnx` and ensure there are 0 warnings.
- [x] Ensure Docker is running and run `dotnet test src/Tests/Functional.Tests`.
- [x] (Optional Cleanup) Ensure the solution builds cleanly and tests discover correctly in Visual Studio / Test Explorer.
