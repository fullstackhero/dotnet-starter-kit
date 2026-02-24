---
paths:
  - "src/Modules/**"
---

# Module Rules

Modules are **bounded contexts** in the modular monolith. Each module is self-contained.

## Module Structure

```
Modules/{ModuleName}/
├── {ModuleName}.Contracts/        # Public interface (DTOs, events)
│   ├── {Entity}Dto.cs
│   ├── I{Module}Service.cs
│   └── {Module}Events.cs
├── {ModuleName}/                  # Implementation (internal)
│   ├── Features/                  # CQRS features
│   │   └── v1/{Feature}/
│   │       ├── {Action}Command.cs
│   │       ├── {Action}Handler.cs
│   │       ├── {Action}Validator.cs
│   │       └── {Action}Endpoint.cs
│   ├── Entities/                  # Domain models
│   ├── Persistence/               # DbContext, configurations
│   ├── Permissions/               # Permission constants
│   └── Extensions.cs              # DI registration
```

## Module Independence

### ✅ Allowed

```csharp
// Reference Contracts project
using FSH.Modules.Identity.Contracts;

public record UserDto(Guid Id, string Email);  // Public DTO
```

```csharp
// Use BuildingBlocks
using FSH.BuildingBlocks.Core;
using FSH.BuildingBlocks.Persistence;
```

### ❌ Forbidden

```csharp
// Direct reference to another module's internals
using FSH.Modules.Identity;  // ❌ NO! Use .Contracts instead

using FSH.Modules.Identity.Entities;  // ❌ Domain models are internal
```

## Communication Between Modules

### Option 1: Contracts (Preferred)

**Identity.Contracts:**
```csharp
public interface IUserService
{
    Task<UserDto?> GetUserByIdAsync(Guid userId);
}
```

**Identity implementation:**
```csharp
internal class UserService : IUserService
{
    public async Task<UserDto?> GetUserByIdAsync(Guid userId)
    {
        // Query database
        return userDto;
    }
}
```

**Other module uses it:**
```csharp
public class OrderHandler(IUserService userService)
{
    public async ValueTask Handle(...)
    {
        var user = await userService.GetUserByIdAsync(userId);
    }
}
```

### Option 2: Domain Events

**Identity module raises event:**
```csharp
public record UserCreatedEvent(Guid UserId, string Email) : DomainEvent;

// In handler
await eventBus.PublishAsync(new UserCreatedEvent(user.Id, user.Email));
```

**Other module subscribes:**
```csharp
public class UserCreatedEventHandler : IEventHandler<UserCreatedEvent>
{
    public async Task Handle(UserCreatedEvent evt, CancellationToken ct)
    {
        // React to user creation (e.g., send welcome email)
    }
}
```

## Creating a New Module

### 1. Create Projects

```bash
# Contracts (public interface)
dotnet new classlib -n FSH.Modules.Catalog.Contracts -o src/Modules/Catalog/Modules.Catalog.Contracts

# Implementation (internal)
dotnet new classlib -n FSH.Modules.Catalog -o src/Modules/Catalog/Modules.Catalog
```

### 2. Add to Solution

```bash
dotnet sln src/FSH.Framework.slnx add \
    src/Modules/Catalog/Modules.Catalog.Contracts/Modules.Catalog.Contracts.csproj \
    src/Modules/Catalog/Modules.Catalog/Modules.Catalog.csproj
```

### 3. Reference BuildingBlocks

```xml
<!-- In Modules.Catalog.csproj -->
<ItemGroup>
  <ProjectReference Include="..\..\BuildingBlocks\Core\Core.csproj" />
  <ProjectReference Include="..\..\BuildingBlocks\Persistence\Persistence.csproj" />
  <ProjectReference Include="..\Modules.Catalog.Contracts\Modules.Catalog.Contracts.csproj" />
</ItemGroup>
```

### 4. Create Entities

```csharp
namespace FSH.Modules.Catalog.Entities;

public class Product : BaseEntity, IAuditable, IMustHaveTenant
{
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public Money Price { get; private set; } = default!;
    public Guid TenantId { get; set; }
    
    public static Product Create(string name, string description, Money price)
    {
        return new Product 
        { 
            Name = name, 
            Description = description, 
            Price = price 
        };
    }
    
    public void Update(string name, string description, Money price)
    {
        Name = name;
        Description = description;
        Price = price;
    }
}
```

### 5. Create DbContext

```csharp
namespace FSH.Modules.Catalog.Persistence;

public class CatalogDbContext(DbContextOptions<CatalogDbContext> options) 
    : BaseDbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("catalog");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

### 6. Create Entity Configuration

```csharp
namespace FSH.Modules.Catalog.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products", "catalog");
        
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.OwnsOne(p => p.Price, price =>
        {
            price.Property(m => m.Amount).HasColumnName("price_amount");
            price.Property(m => m.Currency).HasColumnName("price_currency");
        });
    }
}
```

### 7. Register Module (Extensions.cs)

```csharp
namespace FSH.Modules.Catalog;

public static class Extensions
{
    public static IServiceCollection AddCatalogModule(this IServiceCollection services)
    {
        // Register DbContext
        services.AddDbContext<CatalogDbContext>();
        
        // Register repositories
        services.AddScoped<IRepository<Product>, Repository<Product>>();
        
        // Register services (if any)
        // services.AddScoped<ICatalogService, CatalogService>();
        
        return services;
    }
    
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/catalog")
            .WithTags("Catalog");
        
        // Map feature endpoints here
        // group.MapCreateProductEndpoint();
        
        return endpoints;
    }
}
```

### 8. Wire Up in Program.cs

```csharp
// In Playground.Api/Program.cs
builder.Services.AddCatalogModule();

// ...

app.MapCatalogEndpoints();
```

## Module Boundaries

### Namespace Convention

- **Public:** `FSH.Modules.{Module}.Contracts`
- **Internal:** `FSH.Modules.{Module}.*`

### Assembly Internals

Mark module types as `internal` unless explicitly needed externally:

```csharp
internal class ProductService { }  // ✅ Internal by default
public record ProductDto { }       // ✅ Public DTO in Contracts
```

### Dependency Direction

```
Other Modules → Module.Contracts
                     ↑
                Module (implements Contracts)
                     ↑
                BuildingBlocks
```

**Never:**
- Module A → Module B (direct reference)
- Module → Playground (implementation referencing host)

## Testing Modules

**Architecture Test:**
```csharp
[Fact]
public void Catalog_Module_Should_Not_Reference_Identity_Module()
{
    var catalog = Types.InAssembly(typeof(CatalogDbContext).Assembly);
    var identity = Types.InAssembly(typeof(IdentityDbContext).Assembly);
    
    catalog.Should().NotHaveDependencyOn(identity.Assemblies);
}
```

**Unit Test:**
```csharp
public class ProductTests
{
    [Fact]
    public void Create_Should_Set_Properties()
    {
        var product = Product.Create("Test", "Description", new Money(100, "USD"));
        
        product.Name.Should().Be("Test");
        product.Price.Amount.Should().Be(100);
    }
}
```

## Common Patterns

### Permissions

```csharp
namespace FSH.Modules.Catalog.Permissions;

public static class CatalogPermissions
{
    public static class Products
    {
        public const string View = "catalog.products.view";
        public const string Create = "catalog.products.create";
        public const string Update = "catalog.products.update";
        public const string Delete = "catalog.products.delete";
    }
}
```

### DTOs (in Contracts)

```csharp
namespace FSH.Modules.Catalog.Contracts;

public record ProductDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    DateTime CreatedAt);
```

### Events (in Contracts)

```csharp
namespace FSH.Modules.Catalog.Contracts;

public record ProductCreatedEvent(Guid ProductId, string Name) : DomainEvent;
public record ProductUpdatedEvent(Guid ProductId) : DomainEvent;
public record ProductDeletedEvent(Guid ProductId) : DomainEvent;
```

## Key Rules

1. **Contracts are public**, internals are `internal`
2. **Modules communicate via Contracts or events**, never direct references
3. **Each module has its own DbContext**
4. **Features are vertical slices** within modules
5. **BuildingBlocks are shared**, modules are independent

---

For scaffolding help: Use `/add-module` skill or `module-creator` agent.
