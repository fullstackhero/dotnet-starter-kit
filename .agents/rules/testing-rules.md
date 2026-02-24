---
paths:
  - "src/Tests/**/*"
---

# Testing Rules

Rules for tests in FSH.

## Test Organization

```
src/Tests/
├── Architecture.Tests/    # Layering enforcement (mandatory)
├── {Module}.Tests/        # Module-specific tests
└── Generic.Tests/         # Shared utilities
```

## Naming Conventions

| Type | Pattern |
|------|---------|
| Test class | `{ClassUnderTest}Tests` |
| Test method | `{Method}_{Scenario}_{ExpectedResult}` |
| Test file | Same as class name |

## Test Structure

Always use Arrange-Act-Assert:

```csharp
[Fact]
public async Task Handle_ValidCommand_ReturnsId()
{
    // Arrange
    var command = new CreateProductCommand("Test", 10m);
    
    // Act
    var result = await _handler.Handle(command, CancellationToken.None);
    
    // Assert
    result.Id.Should().NotBeEmpty();
}
```

## Required Tests

### For Handlers
- Happy path with valid input
- Edge cases (empty, null, boundary values)
- Repository interactions verified

### For Validators
- Each validation rule has a test
- Valid input passes
- Invalid input fails with correct property

### For Entities
- Factory method creates valid entity
- Invalid input throws appropriate exception
- Domain events raised correctly

## Libraries

- **xUnit** - Test framework
- **FluentAssertions** - `.Should()` assertions
- **Moq** - `Mock<T>` for dependencies

## Architecture Tests

Architecture tests in `Architecture.Tests/` are mandatory and enforce:
- Module boundary isolation
- No cross-module internal dependencies
- Handlers/validators are sealed
- Contracts don't depend on implementations

These run on every build and PR.
