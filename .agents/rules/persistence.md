---
paths:
  - "src/**/Persistence/**"
  - "src/**/Entities/**"
---

# Persistence Rules

EF Core patterns and repository usage in FSH.

## DbContext Pattern

### One DbContext Per Module

```csharp
namespace FSH.Modules.Catalog.Persistence;

public class CatalogDbContext(DbContextOptions<CatalogDbContext> options) 
    : BaseDbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("catalog");  // ✅ Module-specific schema
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

### BaseDbContext Features

Inherited from `BuildingBlocks.Persistence`:
- Automatic tenant filtering
- Audit trail (Created/Modified timestamps)
- Soft delete support
- Domain event publishing

## Entity Configuration

### Use Fluent API (NOT Data Annotations)

```csharp
namespace FSH.Modules.Catalog.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products", "catalog");
        
        // Primary key
        builder.HasKey(p => p.Id);
        
        // Properties
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(p => p.Description)
            .HasMaxLength(2000);
        
        // Value object (owned type)
        builder.OwnsOne(p => p.Price, price =>
        {
            price.Property(m => m.Amount)
                .HasColumnName("price_amount")
                .HasPrecision(18, 2);
            
            price.Property(m => m.Currency)
                .HasColumnName("price_currency")
                .HasMaxLength(3);
        });
        
        // Relationships
        builder.HasOne(p => p.Category)
            .WithMany()
            .HasForeignKey(p => p.CategoryId);
        
        // Indexes
        builder.HasIndex(p => p.Name);
        builder.HasIndex(p => p.TenantId);  // ✅ For multi-tenancy
    }
}
```

## Repository Pattern

### Generic Repository (Provided by BuildingBlocks)

```csharp
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<T>> ListAsync(CancellationToken ct = default);
    Task<List<T>> ListAsync(Specification<T> spec, CancellationToken ct = default);
    Task<T> AddAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task DeleteAsync(T entity, CancellationToken ct = default);
    Task<int> CountAsync(Specification<T> spec, CancellationToken ct = default);
    Task<bool> AnyAsync(Specification<T> spec, CancellationToken ct = default);
}
```

### Usage in Handlers

```csharp
public class CreateProductHandler(IRepository<Product> productRepo) 
    : ICommandHandler<CreateProductCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateProductCommand cmd, CancellationToken ct)
    {
        var product = Product.Create(cmd.Name, cmd.Description, cmd.Price);
        
        await productRepo.AddAsync(product, ct);
        
        return product.Id;
    }
}
```

## Specification Pattern

### Creating Specifications

```csharp
namespace FSH.Modules.Catalog.Specifications;

public class ProductsByNameSpec : Specification<Product>
{
    public ProductsByNameSpec(string searchTerm)
    {
        Query
            .Where(p => p.Name.Contains(searchTerm))
            .OrderBy(p => p.Name);
    }
}

public class ActiveProductsSpec : Specification<Product>
{
    public ActiveProductsSpec()
    {
        Query
            .Where(p => !p.IsDeleted && p.IsActive)
            .Include(p => p.Category)
            .OrderByDescending(p => p.CreatedAt);
    }
}
```

### Using Specifications

```csharp
public class GetProductsHandler(IRepository<Product> repo) 
    : IQueryHandler<GetProductsQuery, List<ProductDto>>
{
    public async ValueTask<List<ProductDto>> Handle(GetProductsQuery query, CancellationToken ct)
    {
        var spec = new ActiveProductsSpec();
        var products = await repo.ListAsync(spec, ct);
        
        return products.Select(p => p.ToDto()).ToList();
    }
}
```

### Pagination Specification

```csharp
public class ProductsPaginatedSpec : Specification<Product>
{
    public ProductsPaginatedSpec(int pageNumber, int pageSize)
    {
        Query
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);
    }
}
```

## Entity Base Classes

### BaseEntity

```csharp
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public Guid? ModifiedBy { get; set; }
}
```

### IAuditable

```csharp
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    Guid? CreatedBy { get; set; }
    DateTime? ModifiedAt { get; set; }
    Guid? ModifiedBy { get; set; }
}
```

### IMustHaveTenant

```csharp
public interface IMustHaveTenant
{
    Guid TenantId { get; set; }  // ✅ Automatically filtered by Finbuckle
}
```

### ISoftDelete

```csharp
public interface ISoftDelete
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    Guid? DeletedBy { get; set; }
}
```

## Multi-Tenancy

### Tenant-Aware Entities

```csharp
public class Order : BaseEntity, IAuditable, IMustHaveTenant
{
    public Guid TenantId { get; set; }  // ✅ Required for tenant isolation
    public string OrderNumber { get; private set; } = default!;
    public decimal Total { get; private set; }
    
    // ...
}
```

### Global Query Filter (Automatic)

BaseDbContext automatically applies:
```csharp
modelBuilder.Entity<Order>()
    .HasQueryFilter(e => e.TenantId == currentTenantId);
```

**Result:** All queries automatically filter by current tenant. No need to add `.Where(x => x.TenantId == ...)` everywhere.

### Shared Entities (No Tenant)

```csharp
public class Country : BaseEntity  // ❌ No IMustHaveTenant
{
    public string Name { get; private set; } = default!;
    public string Code { get; private set; } = default!;
}
```

## Migrations

### Creating Migrations

```bash
# From solution root
dotnet ef migrations add InitialCatalog \
    --project src/Playground/Migrations.PostgreSQL \
    --context CatalogDbContext \
    --output-dir Migrations/Catalog
```

### Applying Migrations

```bash
# Automatic on startup (Playground.Api)
# Or manually:
dotnet ef database update \
    --project src/Playground/Migrations.PostgreSQL \
    --context CatalogDbContext
```

### Migration Project Pattern

FSH uses a separate migrations project (`Migrations.PostgreSQL`) to:
- Keep migrations out of module code
- Support multiple database providers
- Simplify deployment

## Transactions

### Implicit Transactions

Commands automatically run in a transaction:
```csharp
public async ValueTask<Guid> Handle(CreateOrderCommand cmd, CancellationToken ct)
{
    var order = Order.Create(...);
    await orderRepo.AddAsync(order, ct);
    
    var payment = Payment.Create(...);
    await paymentRepo.AddAsync(payment, ct);
    
    // ✅ Both saved in one transaction automatically
    return order.Id;
}
```

### Explicit Transactions

```csharp
await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

try
{
    await orderRepo.AddAsync(order, ct);
    await paymentRepo.AddAsync(payment, ct);
    
    await transaction.CommitAsync(ct);
}
catch
{
    await transaction.RollbackAsync(ct);
    throw;
}
```

## Performance Patterns

### Projection (DTO Mapping)

```csharp
// ❌ Bad: Load full entity, map in memory
var products = await repo.ListAsync(spec, ct);
return products.Select(p => new ProductDto(...)).ToList();

// ✅ Good: Project in database
var query = dbContext.Products
    .Where(p => !p.IsDeleted)
    .Select(p => new ProductDto(p.Id, p.Name, p.Price.Amount));
return await query.ToListAsync(ct);
```

### AsNoTracking for Read-Only

```csharp
public class ProductsReadOnlySpec : Specification<Product>
{
    public ProductsReadOnlySpec()
    {
        Query
            .AsNoTracking()  // ✅ Faster for queries
            .Where(p => !p.IsDeleted);
    }
}
```

### Batch Operations

```csharp
// ✅ Good: Batch delete
await dbContext.Products
    .Where(p => p.CategoryId == categoryId)
    .ExecuteDeleteAsync(ct);

// ✅ Good: Batch update
await dbContext.Products
    .Where(p => p.CategoryId == categoryId)
    .ExecuteUpdateAsync(p => p.SetProperty(x => x.IsActive, false), ct);
```

## Common Pitfalls

### ❌ Tracking Issues

```csharp
// ❌ Don't detach entities manually
dbContext.Entry(product).State = EntityState.Detached;

// ✅ Use repository pattern
await repo.UpdateAsync(product, ct);
```

### ❌ N+1 Queries

```csharp
// ❌ Bad: N+1
var orders = await repo.ListAsync(ct);
foreach (var order in orders)
{
    var customer = await customerRepo.GetByIdAsync(order.CustomerId, ct);  // N queries!
}

// ✅ Good: Eager loading
var spec = new OrdersWithCustomersSpec();  // Includes .Include(o => o.Customer)
var orders = await repo.ListAsync(spec, ct);
```

### ❌ Lazy Loading

```csharp
// ❌ Lazy loading is DISABLED in FSH
var order = await repo.GetByIdAsync(orderId, ct);
var customer = order.Customer;  // ❌ NULL! Not loaded

// ✅ Explicit loading via specification
var spec = new OrderByIdWithCustomerSpec(orderId);
var order = await repo.FirstOrDefaultAsync(spec, ct);
var customer = order.Customer;  // ✅ Loaded
```

## Key Rules

1. **One DbContext per module**, separate schemas
2. **Fluent API for configuration**, not data annotations
3. **Repository pattern for writes**, direct DbContext for complex reads
4. **Specifications for reusable queries**
5. **Tenant isolation is automatic** (via IMustHaveTenant)
6. **Migrations in separate project** (Migrations.PostgreSQL)
7. **AsNoTracking for read-only queries**
8. **Project to DTOs in database** (avoid loading full entities)

---

For migration help: Use `migration-helper` agent or see EF Core docs.
