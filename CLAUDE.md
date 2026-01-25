# CLAUDE.md

> FullStackHero .NET Starter Kit — AI Assistant Guidelines

## Quick Reference

```bash
dotnet build src/FSH.Framework.slnx     # Build (0 warnings required)
dotnet test src/FSH.Framework.slnx      # Test
dotnet run --project src/Playground/FSH.Playground.AppHost  # Run
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

Every feature = 4 files in one folder:

```
Modules/{Module}/Features/v1/{Feature}/
├── {Feature}Command.cs     → ICommand<TResult>
├── {Feature}Handler.cs     → ICommandHandler<TCommand, TResult>
├── {Feature}Validator.cs   → AbstractValidator<TCommand>
└── {Feature}Endpoint.cs    → RouteHandlerBuilder extension
```

## Essential Rules

| Rule | Why |
|------|-----|
| Use `Mediator` not `MediatR` | Different library, different interfaces |
| DTOs in Contracts project | Keep internals internal |
| Every command needs validator | No unvalidated input |
| `.RequirePermission()` on endpoints | Explicit authorization |
| Zero build warnings | CI enforces this |

## Deep Dive

| Topic | File |
|-------|------|
| All rules & constraints | [.claude/rules.md](.claude/rules.md) |
| Step-by-step guides | [.claude/skills.md](.claude/skills.md) |
| AI behavior guidelines | [.claude/agents.md](.claude/agents.md) |

---

## Quick Patterns

### Command + Handler
```csharp
public sealed record CreateUserCommand(string Email, string Name) : ICommand<Guid>;

public sealed class CreateUserHandler(IRepository<User> repo) 
    : ICommandHandler<CreateUserCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateUserCommand cmd, CancellationToken ct)
    {
        var user = User.Create(cmd.Email, cmd.Name);
        await repo.AddAsync(user, ct);
        return user.Id;
    }
}
```

### Endpoint
```csharp
public static RouteHandlerBuilder MapCreateUserEndpoint(this IEndpointRouteBuilder e) =>
    e.MapPost("/", async (CreateUserCommand cmd, IMediator m, CancellationToken ct) =>
        TypedResults.Created($"/users/{await m.Send(cmd, ct)}"))
    .WithName("CreateUser")
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
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
```

---

## Configuration (Production)

```
DatabaseOptions:ConnectionString  ← Required
CachingOptions:Redis              ← Required
JwtOptions:SigningKey             ← Required (256-bit)
```
