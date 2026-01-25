# Copilot Instructions

This is FullStackHero .NET Starter Kit - a modular monolith with vertical slice architecture.

## Key Patterns

**Feature Structure:**
```
Modules/{Module}/Features/v1/{Feature}/
├── {Feature}Command.cs      # ICommand<TResult>
├── {Feature}Handler.cs      # ICommandHandler<TCommand, TResult>
├── {Feature}Validator.cs    # AbstractValidator<TCommand>
└── {Feature}Endpoint.cs     # Static RouteHandlerBuilder extension
```

**Use Mediator (not MediatR):**
```csharp
// ✓ Correct
public sealed record CreateUserCommand(string Email) : ICommand<Guid>;
public sealed class CreateUserHandler : ICommandHandler<CreateUserCommand, Guid>

// ✗ Wrong
public class CreateUserCommand : IRequest<Guid>  // This is MediatR
```

**Endpoints:**
```csharp
public static RouteHandlerBuilder MapCreateUserEndpoint(this IEndpointRouteBuilder e) =>
    e.MapPost("/", async (CreateUserCommand cmd, IMediator m, CancellationToken ct) =>
        TypedResults.Ok(await m.Send(cmd, ct)))
    .WithName("CreateUser")
    .WithSummary("Creates a new user")
    .RequirePermission(IdentityPermissions.Users.Create);
```

## Rules

1. DTOs in `Modules.{Module}.Contracts/` - never return entities
2. Every command needs a FluentValidation validator
3. Use `[AsParameters]` for query parameters
4. Use `.RequirePermission()` for auth
5. Build with 0 warnings: `dotnet build src/FSH.Framework.slnx`

## Project Structure

- `BuildingBlocks/` - Framework (don't modify without approval)
- `Modules/` - Feature modules (Identity, Multitenancy, Auditing)
- `Playground/` - Reference application
- `Tests/` - Architecture and unit tests
