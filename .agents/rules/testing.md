# Testing conventions

Read before writing or changing tests.

## Stack

xUnit · Shouldly (`result.ShouldBe(...)`) · NSubstitute (`Substitute.For<IService>()`) · AutoFixture (`_fixture.Create<T>()`) · NetArchTest (architecture rules) · Testcontainers (integration).

## Naming & shape

- Method name: `MethodName_Should_ExpectedBehavior_When_Condition`.
- Arrange-Act-Assert, grouped with `#region` (Happy Path / Exception / Edge Cases).
- Assert on observable behavior. When verifying a forwarded `CancellationToken`, assert the **specific** token (`Received(1).XAsync(arg, ct)`), not the implicit default — NSubstitute fills optional params with `default`, so `Received(1).XAsync(arg)` silently asserts `CancellationToken.None`.

## Test projects (`src/Tests/`)

| Project | Scope | Docker? |
|---|---|---|
| `{Module}.Tests` | Unit: handlers, services, domain | no |
| `Framework.Tests`, `Generic.Tests`, `Caching.Tests` | BuildingBlocks units | no |
| `Architecture.Tests` | NetArchTest: module boundaries + tenant-isolation rules + handler↔validator pairing | no |
| `Integration.Tests` | `WebApplicationFactory` over real PostgreSQL/Redis/MinIO | **yes** |
| `Integration.Middleware.Tests` | Real middleware wiring | **yes** |

```bash
dotnet test src/FSH.Starter.slnx                 # all (integration needs Docker)
dotnet test src/Tests/{Module}.Tests             # one project
dotnet test --collect "XPlat Code Coverage" --settings coverage.runsettings
```

If Docker is down, integration tests fail fast with `DockerUnavailableException` — that is environmental, not a code regression. Run the unit projects to validate logic.

## Architecture tests (must stay green)

- Modules reference other modules only via `.Contracts`.
- Tenant-isolation rules on entities.
- Every command handler + paginated query handler has a `{Name}Validator` (`HandlerValidatorPairingTests`). Validator names accepted: `{Cmd}Validator`, `{Name}CommandValidator`, `{Name}Validator`.

## Integration-test gotchas

- Set the Finbuckle tenant context **inline** in the test method (AsyncLocal — an awaited helper loses it → NRE in the tenant filter).
- `AddHeroStorage` reads config eagerly; rewire `IStorageService` **after** registration in the factory.
- SignalR hub tests force **long-polling** (TestServer has no WebSocket).

## Frontend tests

Playwright, route-mocked (no real backend) — see `frontend/shared.md`. `cd clients/{app} && npm run test:e2e`.
