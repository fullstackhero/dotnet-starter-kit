---
name: add-entity
description: Create a domain entity with multi-tenancy, auditing, soft-delete, and domain events. Use when adding new database entities to a module.
argument-hint: [ModuleName] [EntityName]
---

# Add Entity

Create a domain entity following FSH patterns with full multi-tenancy support.

## Entity Template

```csharp
public sealed class {Entity} : AggregateRoot<Guid>, IHasTenant, IAuditableEntity, ISoftDeletable
{
    // Domain properties
    public string Name { get; private set; } = null!;
    public decimal Price { get; private set; }
    public string? Description { get; private set; }

    // IHasTenant - automatic tenant isolation
    public string TenantId { get; private set; } = null!;

    // IAuditableEntity - automatic audit trails
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedAt { get; set; }
    public string? LastModifiedBy { get; set; }

    // ISoftDeletable - automatic soft deletes
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Private constructor for EF Core
    private {Entity}() { }

    // Factory method - the only way to create
    public static {Entity} Create(string name, decimal price, string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(price);

        var entity = new {Entity}
        {
            Id = Guid.NewGuid(),
            Name = name,
            Price = price,
            TenantId = tenantId
        };

        entity.AddDomainEvent(new {Entity}CreatedEvent(entity.Id));
        return entity;
    }

    // Domain methods for state changes
    public void UpdateDetails(string name, decimal price, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(price);

        Name = name;
        Price = price;
        Description = description;

        AddDomainEvent(new {Entity}UpdatedEvent(Id));
    }
}
```

## Domain Events

```csharp
public sealed record {Entity}CreatedEvent(Guid {Entity}Id) : IDomainEvent;
public sealed record {Entity}UpdatedEvent(Guid {Entity}Id) : IDomainEvent;
public sealed record {Entity}DeletedEvent(Guid {Entity}Id) : IDomainEvent;
```

## EF Core Configuration

```csharp
public sealed class {Entity}Configuration : IEntityTypeConfiguration<{Entity}>
{
    public void Configure(EntityTypeBuilder<{Entity}> builder)
    {
        builder.ToTable("{entities}");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Price)
            .HasPrecision(18, 2);

        builder.Property(x => x.TenantId)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(x => x.TenantId);

        // Global query filter for tenant isolation
        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}
```

## Register in DbContext

```csharp
public sealed class {Module}DbContext : DbContext
{
    public DbSet<{Entity}> {Entities} => Set<{Entity}>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("{module}");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof({Module}DbContext).Assembly);
    }
}
```

## Add Migration

```bash
dotnet ef migrations add Add{Entity} \
  --project src/Playground/Migrations.PostgreSQL \
  --startup-project src/Playground/Playground.Api

dotnet ef database update \
  --project src/Playground/Migrations.PostgreSQL \
  --startup-project src/Playground/Playground.Api
```

## Interfaces Reference

| Interface | Purpose | Auto-Handled |
|-----------|---------|--------------|
| `IHasTenant` | Tenant isolation | Query filtering |
| `IAuditableEntity` | Created/Modified tracking | SaveChanges interceptor |
| `ISoftDeletable` | Soft delete support | Delete interceptor |
| `AggregateRoot<T>` | Domain events support | Event dispatcher |

## Key Rules

1. **Private constructor** - EF Core needs it, but users use factory methods
2. **Factory methods** - All creation goes through `Create()` static method
3. **Domain methods** - State changes through methods, not property setters
4. **Domain events** - Raise events for significant state changes
5. **Validation in methods** - Validate in factory/domain methods, not entity
6. **No public setters** - Properties are `private set`

## Checklist

- [ ] Implements `AggregateRoot<Guid>`
- [ ] Implements `IHasTenant` for tenant isolation
- [ ] Implements `IAuditableEntity` for audit trails
- [ ] Implements `ISoftDeletable` for soft deletes
- [ ] Has private constructor
- [ ] Has static factory method
- [ ] Domain events raised for state changes
- [ ] EF configuration created
- [ ] Added to DbContext
- [ ] Migration created
