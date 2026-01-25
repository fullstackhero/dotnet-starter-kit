# CLAUDE.md

> AI assistant guidelines for FullStackHero .NET Starter Kit

## Philosophy

This is a **modular monolith** using **vertical slice architecture**. Every decision optimizes for:

1. **Feature isolation** — Each feature is self-contained in one folder
2. **Module boundaries** — Modules are deployment-ready packages
3. **Explicit over implicit** — No magic, clear dependency flow
4. **Contract-first** — Public APIs via Contracts projects, internals stay internal

## Mental Model

```
┌─────────────────────────────────────────────────────────────┐
│  Playground (Reference App)                                  │
│  └─ Wires modules together, runs the app                    │
├─────────────────────────────────────────────────────────────┤
│  Modules (Feature Packages)                                  │
│  ├─ Identity     → Auth, users, roles, permissions          │
│  ├─ Multitenancy → Tenant isolation, per-tenant DBs         │
│  └─ Auditing     → Audit trail, security logs               │
├─────────────────────────────────────────────────────────────┤
│  BuildingBlocks (Framework)                                  │
│  └─ Core, Persistence, Web, Caching, Jobs, Events...        │
│     ⚠️  Changes here affect ALL modules                      │
└─────────────────────────────────────────────────────────────┘
```

## The Vertical Slice

**One feature = one folder with everything it needs.**

```
Modules/Identity/Features/v1/CreateUser/
├── CreateUserCommand.cs      ← What (input)
├── CreateUserHandler.cs      ← How (logic)  
├── CreateUserValidator.cs    ← Guard (validation)
└── CreateUserEndpoint.cs     ← Where (HTTP binding)
```

**Why this works:**
- Change a feature? One folder to modify
- Delete a feature? Delete one folder
- Understand a feature? Read one folder
- Test a feature? Mock one handler

## Decision Guide

### "Where do I put this?"

| You're building... | Put it in... |
|-------------------|--------------|
| New API endpoint | `Modules/{Module}/Features/v1/{Feature}/` |
| Shared DTO/contract | `Modules.{Module}.Contracts/` |
| Cross-cutting concern | `BuildingBlocks/` (needs approval) |
| New bounded context | New `Modules.{Name}/` project |
| Database migration | `Playground/Migrations.PostgreSQL/` |

### "Should I create a new module?"

Ask: **Does this represent a separate bounded context?**
- Has its own domain entities? → New module
- Could be deployed independently? → New module  
- Just a new feature in existing domain? → Add to existing module

## Code Patterns

### Command/Query (CQRS)

```csharp
// Command — changes state, returns result
public sealed record CreateUserCommand(string Email, string Name) : ICommand<UserCreatedResponse>;

// Query — reads state, no side effects  
public sealed record GetUserQuery(Guid Id) : IQuery<UserDto>;
```

### Handler

```csharp
public sealed class CreateUserHandler(
    IRepository<User> repo,
    ICurrentUser currentUser) : ICommandHandler<CreateUserCommand, UserCreatedResponse>
{
    public async ValueTask<UserCreatedResponse> Handle(CreateUserCommand cmd, CancellationToken ct)
    {
        var user = User.Create(cmd.Email, cmd.Name, currentUser.TenantId);
        await repo.AddAsync(user, ct);
        return new UserCreatedResponse(user.Id);
    }
}
```

### Endpoint

```csharp
public static class CreateUserEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder e) =>
        e.MapPost("/", async (CreateUserCommand cmd, IMediator m, CancellationToken ct) =>
            TypedResults.Created($"/users/{(await m.Send(cmd, ct)).Id}"))
        .WithName(nameof(CreateUserCommand))
        .WithSummary("Create a new user")
        .RequirePermission(IdentityPermissions.Users.Create);
}
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

## Critical Rules

| Rule | Rationale |
|------|-----------|
| Use `Mediator` not `MediatR` | Source-gen library, different interfaces (`ICommand`, `IQuery`) |
| Handlers use primary constructors | Clean DI, immutable dependencies |
| DTOs in Contracts project | Shareable with clients, no internal leakage |
| Entities implement `IHasTenant` | Auto-filtered by tenant, no manual filtering |
| Endpoints use `.RequirePermission()` | Centralized auth, auditable |
| Zero build warnings | CI enforces, no exceptions |

## Multi-Tenancy

Every request has a tenant context. The framework handles:
- **Tenant resolution** — From header, subdomain, or path
- **Data isolation** — Queries auto-filter by `TenantId`
- **Per-tenant DBs** — Optional, configurable per tenant

**Your code:** Just implement `IHasTenant` on entities. Done.

## Commands

```bash
# Build (must pass with 0 warnings)
dotnet build src/FSH.Framework.slnx

# Test (architecture tests enforce patterns)
dotnet test src/FSH.Framework.slnx

# Run with infrastructure (Postgres + Redis via Aspire)
dotnet run --project src/Playground/FSH.Playground.AppHost

# Run API only (needs manual DB/Redis setup)
dotnet run --project src/Playground/Playground.Api
```

## Adding a Feature (Step by Step)

1. **Create folder:** `Modules/{Module}/Features/v1/{FeatureName}/`

2. **Add Command/Query:**
   ```csharp
   public sealed record {Name}Command(...) : ICommand<{Result}>;
   ```

3. **Add Handler:**
   ```csharp
   public sealed class {Name}Handler(...) : ICommandHandler<{Name}Command, {Result}>
   ```

4. **Add Validator** (commands only):
   ```csharp
   public sealed class {Name}Validator : AbstractValidator<{Name}Command>
   ```

5. **Add Endpoint:**
   ```csharp
   public static RouteHandlerBuilder Map{Name}Endpoint(this IEndpointRouteBuilder e) => ...
   ```

6. **Wire in Module:** Add `group.Map{Name}Endpoint();` in `MapEndpoints()`

7. **Test:** `dotnet build && dotnet test`

## Configuration

### Required (Production)
```json
{
  "DatabaseOptions": { "ConnectionString": "..." },
  "CachingOptions": { "Redis": "..." },
  "JwtOptions": { "SigningKey": "..." }
}
```

### The stack
- **.NET 10** with C# latest
- **PostgreSQL** (default) or SQL Server
- **Redis** for distributed caching
- **Hangfire** for background jobs
- **FluentValidation** for input validation
- **OpenTelemetry** for observability
