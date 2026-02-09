---
paths:
  - "src/**"
---

# Architecture Rules

FSH is a **Modular Monolith** — NOT microservices, NOT a traditional layered architecture.

## Core Principles

### 1. Modular Monolith

```
Single deployment unit
    ↓
Multiple bounded contexts (modules)
    ↓
Each module is self-contained
    ↓
Communication via Contracts (interfaces/DTOs)
```

**Modules:**
- Identity (users, roles, permissions)
- Multitenancy (tenants, subscriptions)
- Auditing (audit trails)
- Your business modules (e.g., Catalog, Orders)

**Rules:**
- Modules CANNOT reference other module internals
- Modules CAN reference other module Contracts
- Modules share BuildingBlocks (framework code)

### 2. CQRS (Mediator Library)

**Commands** (write operations):
```csharp
public record CreateUserCommand(string Email) : ICommand<Guid>;

public class CreateUserHandler : ICommandHandler<CreateUserCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateUserCommand cmd, CancellationToken ct)
    {
        // Write to database
        return user.Id;
    }
}
```

**Queries** (read operations):
```csharp
public record GetUserQuery(Guid Id) : IQuery<UserDto>;

public class GetUserHandler : IQueryHandler<GetUserQuery, UserDto>
{
    public async ValueTask<UserDto> Handle(GetUserQuery query, CancellationToken ct)
    {
        // Read from database
        return userDto;
    }
}
```

⚠️ **NOT MediatR:** FSH uses `Mediator` library (different interfaces!)

### 3. Domain-Driven Design

**Entities** inherit `BaseEntity`:
```csharp
public class Product : BaseEntity, IAuditable
{
    public string Name { get; private set; } = default!;
    public Money Price { get; private set; } = default!;
    
    public static Product Create(string name, Money price)
    {
        // Factory method, enforce invariants
        return new Product { Name = name, Price = price };
    }
}
```

**Value Objects** (immutable):
```csharp
public record Money(decimal Amount, string Currency);
```

**Aggregates:**
- Root entity controls access to child entities
- Enforce business rules
- Transaction boundary

### 4. Multi-Tenancy

**Finbuckle.MultiTenant:**
- Shared database, tenant isolation via TenantId
- Automatic query filtering
- Tenant resolution from HTTP header or claim

```csharp
// Tenant-aware entity
public class Order : BaseEntity, IMustHaveTenant
{
    public Guid TenantId { get; set; }  // Auto-filtered
}
```

**Tenant Resolution Order:**
1. HTTP header: `X-Tenant`
2. JWT claim: `tenant`
3. Host/route strategy (optional)

### 5. Vertical Slice Architecture

Each feature = complete slice (command/handler/validator/endpoint in one folder).

```
Features/v1/CreateProduct/
├── CreateProductCommand.cs
├── CreateProductHandler.cs
├── CreateProductValidator.cs
└── CreateProductEndpoint.cs
```

**Benefits:**
- High cohesion (related code together)
- Low coupling (features don't depend on each other)
- Easy to find/modify

### 6. BuildingBlocks (Shared Kernel)

11 packages providing cross-cutting concerns:

| Package | Purpose |
|---------|---------|
| Core | Base entities, interfaces, exceptions |
| Persistence | EF Core, repositories, specifications |
| Caching | Redis/memory caching |
| Mailing | Email templates, MailKit integration |
| Jobs | Hangfire background jobs |
| Storage | File storage (AWS S3, local) |
| Web | API conventions, filters, middleware |
| Eventing | Domain events, message bus |
| Blazor.UI | UI components (optional) |
| Shared | DTOs, constants |
| Eventing.Abstractions | Event contracts |

**Protected:** BuildingBlocks should NOT be modified without approval. See `.claude/rules/buildingblocks-protection.md`.

### 7. Dependency Flow

```
API Layer (Minimal APIs)
    ↓
Application Layer (Commands/Queries/Handlers)
    ↓
Domain Layer (Entities/Value Objects)
    ↓
Infrastructure Layer (Persistence/External Services)
```

**Rules:**
- Domain CANNOT depend on infrastructure
- Application CANNOT depend on infrastructure directly
- Infrastructure implements domain interfaces

### 8. Persistence Strategy

**DbContext per Module:**
- IdentityDbContext
- MultitenancyDbContext
- AuditingDbContext
- Your module DbContexts

**Repository Pattern:**
```csharp
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<List<T>> ListAsync(Specification<T> spec, CancellationToken ct);
    Task<T> AddAsync(T entity, CancellationToken ct);
    Task UpdateAsync(T entity, CancellationToken ct);
    Task DeleteAsync(T entity, CancellationToken ct);
}
```

**Specification Pattern** (queries):
```csharp
public class ActiveProductsSpec : Specification<Product>
{
    public ActiveProductsSpec()
    {
        Query.Where(p => !p.IsDeleted && p.IsActive);
    }
}
```

## Architectural Tests

`Architecture.Tests` project enforces rules:

```csharp
[Fact]
public void Modules_Should_Not_Reference_Other_Modules()
{
    // Fails if Module A references Module B directly
}

[Fact]
public void Domain_Should_Not_Depend_On_Infrastructure()
{
    // Fails if domain entities reference EF Core
}
```

## Technology Stack

- **.NET 10** (latest LTS)
- **EF Core 10** (PostgreSQL provider)
- **Mediator** (CQRS)
- **FluentValidation** (input validation)
- **Mapster** (object mapping)
- **Hangfire** (background jobs)
- **Finbuckle.MultiTenant** (multi-tenancy)
- **MailKit** (email)
- **Scalar** (OpenAPI docs)
- **Serilog** (logging)
- **OpenTelemetry** (observability)
- **Aspire** (orchestration)

## Key Takeaways

1. **Modular Monolith** ≠ Microservices. Modules share process, database, infrastructure.
2. **CQRS** separates reads/writes. Use `ICommand`/`IQuery`, not `IRequest`.
3. **DDD** enforces business rules in domain. Entities control their state.
4. **Multi-Tenancy** is built-in. Every entity is either tenant-aware or shared.
5. **Vertical Slices** keep features independent. No shared "services" layer.
6. **BuildingBlocks** provide infrastructure. Don't reinvent, reuse.
7. **Tests enforce architecture**. Violate rules → build fails.

---

For implementation details, see:
- `ARCHITECTURE_ANALYSIS.md` (deep dive)
- `.claude/rules/modules.md` (module patterns)
- `.claude/rules/persistence.md` (data access patterns)
