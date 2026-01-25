# CLAUDE.md

> FullStackHero .NET Starter Kit — AI Assistant Guidelines

## Quick Reference

```bash
dotnet build src/FSH.Framework.slnx     # Build (0 warnings required)
dotnet test src/FSH.Framework.slnx      # Test
dotnet run --project src/Playground/FSH.Playground.AppHost  # Run with Aspire
```

## Project Structure

```
src/
├── BuildingBlocks/     # Framework core (⚠️ don't modify without approval)
├── Modules/            # Business modules — add features here
│   ├── Identity/       # Auth, users, roles, permissions
│   ├── Multitenancy/   # Tenant management
│   └── Auditing/       # Audit logging
├── Playground/         # Reference application
└── Tests/              # Architecture + unit tests
```

## The Pattern

Every feature = vertical slice in one folder:

```
Modules/{Module}/Features/v1/{Feature}/
├── {Action}{Entity}Command.cs      # ICommand<T> (NOT IRequest!)
├── {Action}{Entity}Handler.cs      # ICommandHandler<T,R> returns ValueTask
├── {Action}{Entity}Validator.cs    # AbstractValidator<T>
└── {Action}{Entity}Endpoint.cs     # MapPost/Get/Put/Delete
```

## Critical Rules

| Rule | Why |
|------|-----|
| Use `Mediator` not `MediatR` | Different library, different interfaces |
| `ICommand<T>` / `IQuery<T>` | NOT `IRequest<T>` |
| `ValueTask<T>` return type | NOT `Task<T>` |
| DTOs in Contracts project | Keep internals internal |
| Every command needs validator | No unvalidated input |
| `.RequirePermission()` on endpoints | Explicit authorization |
| Zero build warnings | CI enforces this |

## Available Skills

| Skill | When to Use |
|-------|-------------|
| `/add-feature` | Creating new API endpoints |
| `/add-module` | Creating new bounded contexts |
| `/add-entity` | Adding domain entities |
| `/query-patterns` | Implementing GET with pagination/filtering |
| `/testing-guide` | Writing tests |

## Available Agents

| Agent | Purpose |
|-------|---------|
| `code-reviewer` | Review changes against FSH patterns |
| `feature-scaffolder` | Generate complete feature files |
| `module-creator` | Scaffold new modules |
| `architecture-guard` | Verify architectural integrity |
| `migration-helper` | Handle EF Core migrations |

## Quick Patterns

### Command + Handler
```csharp
public sealed record CreateUserCommand(string Email) : ICommand<Guid>;

public sealed class CreateUserHandler(IRepository<User> repo) 
    : ICommandHandler<CreateUserCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateUserCommand cmd, CancellationToken ct)
    {
        var user = User.Create(cmd.Email);
        await repo.AddAsync(user, ct);
        return user.Id;
    }
}
```

### Endpoint
```csharp
public static RouteHandlerBuilder Map(this IEndpointRouteBuilder e) =>
    e.MapPost("/", async (CreateUserCommand cmd, IMediator m, CancellationToken ct) =>
        TypedResults.Created($"/users/{await m.Send(cmd, ct)}"))
    .WithName(nameof(CreateUserCommand))
    .WithSummary("Create a new user")
    .RequirePermission(IdentityPermissions.Users.Create);
```

### Validator
```csharp
public sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
```

## Before Committing

```bash
dotnet build src/FSH.Framework.slnx  # Must be 0 warnings
dotnet test src/FSH.Framework.slnx   # All tests pass
```
