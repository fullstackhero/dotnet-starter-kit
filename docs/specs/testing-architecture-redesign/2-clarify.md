# Clarifications: Testing Architecture Redesign

## Unresolved Questions
*(None. All questions were resolved during the previous deep analysis phase).*

## Decisions Made

1. **`InMemoryDatabase` Deprecation:** It was clarified that `InMemoryDatabase` is an anti-pattern for evaluating EF Core configurations since it does not support migrations or relational constraints. **Decision:** We will progressively remove `InMemoryDatabase` usage from existing `*.Tests` in favor of pure mock-based unit tests (`NSubstitute`).
2. **`Testcontainers` Usage:** It was questioned whether there are better alternatives to Testcontainers. **Decision:** Testcontainers (orchestrating ephemeral Docker instances of PostgreSQL and Redis) is the optimal, industry-standard approach to ensure isolated, repeatable integration testing without flaky shared state.
3. **Preventing Code Duplication:** We need to avoid duplicating heavy Testcontainer and WebApplicationFactory infrastructure across Integration, Functional, and Spec tests. **Decision:** We will introduce a new `Tests.Shared` (or `Shared.Tests`) core project that will encapsulate the base database fixtures, authentication helpers, and Docker orchestrators. The other test projects will reference this shared core.
4. **Scope of Functional Testing:** Should we test every single endpoint? **Decision:** No. We will apply the Testing Pyramid. Functional tests will cover the "Critical Path" (e.g., Auth, Tenancy lifecycle, User creation), Integration tests will cover complex Data queries, and Unit tests (Mocks) will cover 100% of business/domain logic.
5. **Initial Test Coverage (Validating the Infrastructure):** How do we prove this new architecture works? **Decision:** We do not need "tests for tests". Instead, we will write **one representative test per new layer** during this implementation phase to prove the wiring is correct:
   - *Functional Layer:* A test hitting `/api/v1/tokens` (Login) to prove `WebApplicationFactory`, `HttpClient`, and Docker DB are routing HTTP successfully.
   - *Integration Layer:* A test directly injecting a `GetTenantStatusQuery` (or similar DB-heavy query) into Mediator to prove DB connection and EF Core translation work without HTTP.
   - *Spec Layer:* We will migrate the existing `SetupSanityCheckTests.cs` to inherit from the new Functional infrastructure.
