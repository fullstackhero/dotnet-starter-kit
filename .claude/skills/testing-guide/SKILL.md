---
name: testing-guide
description: Write unit tests, integration tests, and architecture tests for FSH features. Use when adding tests or understanding the testing strategy.
---

# Testing Guide

FSH uses a layered testing strategy with architecture tests as guardrails.

## Test Project Structure

```
src/Tests/
├── Architecture.Tests/    # Enforces layering rules
├── Generic.Tests/         # Shared test utilities
├── Identity.Tests/        # Identity module tests
├── Multitenancy.Tests/    # Multitenancy module tests
└── Auditing.Tests/        # Auditing module tests
```

## Architecture Tests

Architecture tests enforce module boundaries and layering. They run on every build.

```csharp
public class ArchitectureTests
{
    [Fact]
    public void Modules_ShouldNot_DependOnOtherModules()
    {
        var result = Types.InAssembly(typeof(IdentityModule).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Modules.Multitenancy")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Contracts_ShouldNot_DependOnImplementation()
    {
        var result = Types.InAssembly(typeof(UserDto).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Modules.Identity")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Handlers_ShouldBe_Sealed()
    {
        var result = Types.InAssembly(typeof(IdentityModule).Assembly)
            .That()
            .ImplementInterface(typeof(ICommandHandler<,>))
            .Or()
            .ImplementInterface(typeof(IQueryHandler<,>))
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
```

## Unit Test Patterns

### Handler Tests

```csharp
public class Create{Entity}HandlerTests
{
    private readonly Mock<IRepository<{Entity}>> _repositoryMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Create{Entity}Handler _handler;

    public Create{Entity}HandlerTests()
    {
        _repositoryMock = new Mock<IRepository<{Entity}>>();
        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(x => x.TenantId).Returns("test-tenant");

        _handler = new Create{Entity}Handler(
            _repositoryMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_Returns{Entity}Id()
    {
        // Arrange
        var command = new Create{Entity}Command("Test", 99.99m);
        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<{Entity}>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Id.Should().NotBeEmpty();
        _repositoryMock.Verify(x => x.AddAsync(
            It.Is<{Entity}>(e => e.Name == "Test" && e.Price == 99.99m),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

### Validator Tests

```csharp
public class Create{Entity}ValidatorTests
{
    private readonly Create{Entity}Validator _validator = new();

    [Fact]
    public void Validate_EmptyName_Fails()
    {
        var command = new Create{Entity}Command("", 99.99m);
        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_NegativePrice_Fails()
    {
        var command = new Create{Entity}Command("Test", -1m);
        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Price");
    }

    [Theory]
    [InlineData("Valid Name", 10)]
    [InlineData("Another", 0.01)]
    public void Validate_ValidCommand_Passes(string name, decimal price)
    {
        var command = new Create{Entity}Command(name, price);
        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
```

### Entity Tests

```csharp
public class {Entity}Tests
{
    [Fact]
    public void Create_ValidInput_Creates{Entity}WithEvent()
    {
        var entity = {Entity}.Create("Test", 99.99m, "tenant-1");

        entity.Id.Should().NotBeEmpty();
        entity.Name.Should().Be("Test");
        entity.Price.Should().Be(99.99m);
        entity.TenantId.Should().Be("tenant-1");
        entity.DomainEvents.Should().ContainSingle(e => e is {Entity}CreatedEvent);
    }

    [Fact]
    public void Create_EmptyName_ThrowsArgumentException()
    {
        var act = () => {Entity}.Create("", 99.99m, "tenant-1");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateDetails_ValidInput_UpdatesAndRaisesEvent()
    {
        var entity = {Entity}.Create("Original", 50m, "tenant-1");
        entity.ClearDomainEvents();

        entity.UpdateDetails("Updated", 75m, "New description");

        entity.Name.Should().Be("Updated");
        entity.Price.Should().Be(75m);
        entity.Description.Should().Be("New description");
        entity.DomainEvents.Should().ContainSingle(e => e is {Entity}UpdatedEvent);
    }
}
```

## Running Tests

```bash
# Run all tests
dotnet test src/FSH.Framework.slnx

# Run specific test project
dotnet test src/Tests/Architecture.Tests

# Run with coverage
dotnet test src/FSH.Framework.slnx --collect:"XPlat Code Coverage"

# Run specific test
dotnet test --filter "FullyQualifiedName~Create{Entity}HandlerTests"
```

## Test Conventions

| Convention | Example |
|------------|---------|
| Test class name | `{ClassUnderTest}Tests` |
| Test method name | `{Method}_{Scenario}_{ExpectedResult}` |
| Arrange-Act-Assert | Always use this structure |
| One assertion concept | Multiple asserts OK if same concept |

## Key Rules

1. **Architecture tests are mandatory** - They enforce module boundaries
2. **Validators need tests** - Cover edge cases
3. **Handlers need tests** - Mock dependencies
4. **Entities need tests** - Test factory methods and domain logic
5. **Use FluentAssertions** - `.Should()` syntax
6. **Use Moq for mocking** - `Mock<T>` pattern
