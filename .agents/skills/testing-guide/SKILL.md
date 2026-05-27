---
name: testing-guide
description: Write tests for an FSH feature — xUnit + Shouldly + NSubstitute + AutoFixture, with naming and AAA conventions. Use when adding unit/handler/validator/entity tests. Full rules in .agents/rules/testing.md + integration-testing.md.
---

# Testing Guide

Stack: **xUnit** + **Shouldly** (`.ShouldBe`) + **NSubstitute** (`Substitute.For<>`) + **AutoFixture**
(`new Fixture()`). **Not** Moq, **not** FluentAssertions. Detailed conventions + integration-test gotchas
live in `.agents/rules/testing.md` and `.agents/rules/integration-testing.md`.

## Conventions

- Test class: `public sealed class {Sut}Tests`; SUT field named `_sut`.
- Method name: **`MethodName_Should_ExpectedBehavior[_When_Condition]`**.
- Arrange-Act-Assert with `// Arrange` / `// Act` / `// Assert`; group with `#region` (Happy Path / Guards / Edge Cases).
- Mocks via `Substitute.For<IService>()`; assert calls with `.Received(1).X(arg, Arg.Any<CancellationToken>())`.
- When asserting a forwarded `CancellationToken`, assert the **specific** token, not the default (NSubstitute fills optional params with `default`).

## Handler test

```csharp
public sealed class Create{Entity}CommandHandlerTests
{
    private readonly {X}DbContext _db;            // or Substitute.For<IService>() for service deps
    private readonly Create{Entity}CommandHandler _sut;
    private readonly IFixture _fixture = new Fixture();

    public Create{Entity}CommandHandlerTests()
    {
        _db = /* in-memory or test DbContext */;
        _sut = new Create{Entity}CommandHandler(_db);
    }

    [Fact]
    public async Task Handle_Should_PersistEntity_And_ReturnId()
    {
        // Arrange
        var command = new Create{Entity}Command(_fixture.Create<string>(), 9.99m, "USD");

        // Act
        var id = await _sut.Handle(command, CancellationToken.None);

        // Assert
        id.ShouldNotBe(Guid.Empty);
    }
}
```

Service-dependency example (NSubstitute):

```csharp
_userService = Substitute.For<IUserService>();
// Act … then:
await _userService.Received(1).ToggleStatusAsync(true, command.UserId, Arg.Any<CancellationToken>());
```

## Validator test

```csharp
public sealed class Create{Entity}CommandValidatorTests
{
    private readonly Create{Entity}CommandValidator _sut = new();

    [Theory]
    [InlineData("")]
    public void Validate_Should_Fail_When_NameInvalid(string name)
    {
        var result = _sut.Validate(new Create{Entity}Command(name, 1m, "USD"));
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(Create{Entity}Command.Name));
    }
}
```

## Entity / domain test (no mocks)

```csharp
[Fact]
public void Create_Should_RaiseCreatedEvent()
{
    var entity = {Entity}.Create("Test", Money.Zero());
    entity.Id.ShouldNotBe(Guid.Empty);
    entity.DomainEvents.ShouldContain(e => e is {Entity}CreatedDomainEvent);
}
```

## Architecture tests (guardrails — keep green)

`Architecture.Tests` (NetArchTest) enforce: module boundaries (cross-module refs only via `.Contracts`),
tenant-isolation rules, handlers `sealed`, and **every command/paginated-query handler has a validator**.
Don't weaken these to make a change pass — fix the code.

## Integration tests

`Integration.Tests` runs over real Postgres/Redis/MinIO via Testcontainers — **Docker required**. Set the
Finbuckle tenant context inline, rewire `IStorageService` post-registration for MinIO, force long-polling
for SignalR. All detailed in `.agents/rules/integration-testing.md`.

## Run

```bash
dotnet test src/Tests/{X}.Tests
dotnet test src/Tests/Architecture.Tests
dotnet test src/FSH.Starter.slnx --collect "XPlat Code Coverage" --settings coverage.runsettings
```
