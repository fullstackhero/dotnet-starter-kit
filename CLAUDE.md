# FullStackHero .NET Starter Kit

> A production-ready modular .NET framework for building enterprise applications.

## Architecture

**Modular Monolith + Vertical Slice Architecture (VSA)**

- **BuildingBlocks** (`src/BuildingBlocks/`) — shared framework libraries (Core, Persistence, Web, Caching, Eventing, etc.)
- **Modules** (`src/Modules/`) — bounded contexts (Identity, Multitenancy, Auditing)
- **Playground** (`src/Playground/`) — sample host applications (API, Blazor, AppHost)
- **Tests** (`src/Tests/`) — per-module test projects + architecture tests

### Module Boundaries

Modules communicate through **Contracts** projects only. A module MUST NOT reference another module's runtime project.

```
Modules.Identity/           ← runtime (internal)
Modules.Identity.Contracts/ ← public API (commands, queries, events, DTOs, service interfaces)
```

### Feature Folder Layout

Each feature is a vertical slice inside `Features/v{version}/{Area}/{FeatureName}/`:

```
Features/v1/Users/RegisterUser/
├── RegisterUserEndpoint.cs          # Minimal API endpoint
├── RegisterUserCommandHandler.cs    # CQRS handler
└── RegisterUserCommandValidator.cs  # FluentValidation
```

Additional module folders: `Domain/`, `Data/`, `Services/`, `Events/`, `Authorization/`.

## Tech Stack

| Concern | Technology |
|---------|-----------|
| Framework | .NET 10 / C# latest |
| Solution format | `.slnx` (XML-based) |
| Package management | Central (`Directory.Packages.props`) |
| CQRS / Mediator | Mediator 3.0.1 (source generator) |
| Validation | FluentValidation 12.x |
| ORM | Entity Framework Core 10.x |
| Database | PostgreSQL (Npgsql) |
| Auth | JWT Bearer + ASP.NET Identity |
| Multitenancy | Finbuckle.MultiTenant 10.x (claim/header/query strategies) |
| Caching | Redis (StackExchange) |
| Jobs | Hangfire |
| Logging | Serilog + OpenTelemetry (OTLP) |
| Object mapping | Mapster |
| API docs | OpenAPI + Scalar |
| API versioning | Asp.Versioning |
| Hosting | .NET Aspire (AppHost) |
| Blazor UI | MudBlazor |
| Testing | xUnit, Shouldly, NSubstitute, AutoFixture, NetArchTest |

## Build & Run

```bash
# Build
dotnet build src/FSH.Framework.slnx

# Run API (from repo root)
dotnet run --project src/Playground/Playground.Api

# Run with Aspire
dotnet run --project src/Playground/FSH.Playground.AppHost

# Run tests
dotnet test src/FSH.Framework.slnx
```

## Key Conventions

### Endpoints

Static extension methods on `IEndpointRouteBuilder`. Return `RouteHandlerBuilder`.

```csharp
public static class RegisterUserEndpoint
{
    internal static RouteHandlerBuilder MapRegisterUserEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/register", (RegisterUserCommand command,
            IMediator mediator, CancellationToken cancellationToken) =>
            mediator.Send(command, cancellationToken))
            .WithName("RegisterUser")
            .WithSummary("Register user")
            .RequirePermission(IdentityPermissionConstants.Users.Create);
    }
}
```

### CQRS

- **Commands/Queries** → defined in `Modules.{Name}.Contracts` (implement `ICommand<TResponse>` / `IQuery<TResponse>`)
- **Handlers** → defined in `Modules.{Name}/Features/` (implement `ICommandHandler<T, TResponse>` / `IQueryHandler<T, TResponse>`)
- Handlers return `ValueTask<T>` and use `.ConfigureAwait(false)`

### Validation

FluentValidation validators are auto-registered by `ModuleLoader`. Name them `{Command}Validator`.

### Domain Events

- Inherit from `DomainEvent` (abstract record with `EventId`, `OccurredOnUtc`, `CorrelationId`, `TenantId`)
- Entities implement `IHasDomainEvents` with `_domainEvents` list
- Integration events implement `IIntegrationEvent`, handlers implement `IIntegrationEventHandler<T>`

### Domain Entities

- `BaseEntity` — `Id`, `CreatedAt`, `UpdatedAt`, `TenantId`
- `AggregateRoot` — extends `BaseEntity` with domain events
- `IHasTenant`, `IAuditableEntity`, `ISoftDeletable` — marker interfaces

### Module Registration

Each module implements `IModule` with `[FshModule(Order = n)]` attribute:

```csharp
[FshModule(Order = 1)]
public class IdentityModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder) { ... }
    public void MapEndpoints(IEndpointRouteBuilder endpoints) { ... }
}
```

Endpoints are grouped under versioned API paths: `api/v{version:apiVersion}/{module}`.

### Exceptions

Use framework exception types: `CustomException` (with `HttpStatusCode`), `NotFoundException`, `ForbiddenException`, `UnauthorizedException`. Global handler converts to `ProblemDetails` (RFC 9457).

### Permissions

Constants in `Shared/Identity/IdentityPermissionConstants.cs`. Applied via `.RequirePermission()` on endpoints.

### Specifications

Use `Specification<T>` base class from `Persistence/Specifications/` for query composition. Default `AsNoTracking = true`.

## Coding Style

- **Namespace style**: File-scoped (`namespace X;`)
- **Indentation**: 4 spaces
- **Var usage**: Prefer explicit types; `var` only when type is apparent from RHS
- **Null checks**: `is null` / `is not null` (not `== null`)
- **Pattern matching**: Preferred over `is`/`as` casts
- **Switch expressions**: Preferred
- **Async**: `ValueTask<T>` for handlers, `.ConfigureAwait(false)` on all awaits
- **Guard clauses**: `ArgumentNullException.ThrowIfNull(param)` at method entry
- **Properties**: Prefer auto-properties, `default!` for required non-nullable strings
- **Records**: Use for DTOs, events, and value objects

## Testing Conventions

- **Naming**: `MethodName_Should_ExpectedBehavior_When_Condition`
- **Pattern**: Arrange-Act-Assert with `#region` grouping (Happy Path, Exception, Edge Cases)
- **Assertions**: Shouldly (`result.ShouldBe(...)`, `result.ShouldNotBeNull()`)
- **Mocking**: NSubstitute (`Substitute.For<IService>()`)
- **Test data**: AutoFixture (`_fixture.Create<string>()`)
- **Architecture tests**: NetArchTest enforces module boundary rules

## Protected Directories

**DO NOT modify BuildingBlocks** without explicit approval. These are shared framework libraries consumed by all modules. Changes here have wide blast radius.

## Adding a New Feature

1. Add command/query + response in `Modules.{Name}.Contracts/v1/{Area}/{Feature}/`
2. Add handler in `Modules.{Name}/Features/v1/{Area}/{Feature}/`
3. Add validator in the same feature folder
4. Add endpoint in the same feature folder
5. Wire endpoint in the module's `MapEndpoints()` method
6. Add tests in `Tests/{Name}.Tests/`

## Adding a New Module

1. Create `Modules.{Name}/` and `Modules.{Name}.Contracts/` projects under `src/Modules/{Name}/`
2. Implement `IModule` with `[FshModule(Order = n)]`
3. Add DbContext extending from framework base
4. Register in `Program.cs` module assemblies array
5. Add migration project if needed
6. Add test project in `src/Tests/`
7. Add architecture test rules
