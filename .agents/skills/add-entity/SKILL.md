---
name: add-entity
description: Add a domain entity/aggregate with EF configuration and a migration to an existing FSH module. Use when adding a new database-backed entity. Pairs with add-feature and create-migration.
argument-hint: [ModuleName] [EntityName]
---

# Add Entity

Rich domain model: `sealed` aggregate, private EF ctor, static factory, behavior via methods, domain
events. DB conventions: `.agents/rules/database.md`.

## Entity — `AggregateRoot<Guid>` (or `BaseEntity<Guid>`)

`BaseEntity<TId>` gives only `Id` + domain-event machinery. Audit/tenant/soft-delete are **opt-in via
marker interfaces** (the base does NOT carry those fields). New ids use **`Guid.CreateVersion7()`**.

```csharp
public sealed class {Entity} : AggregateRoot<Guid>, IHasTenant, IAuditableEntity, ISoftDeletable
{
    public string Name { get; private set; } = default!;
    public Money Price { get; private set; } = default!;

    // IHasTenant
    public string TenantId { get; private set; } = default!;
    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    // ISoftDeletable
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }

    private {Entity}() { }   // EF

    public static {Entity} Create(string name, Money price)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(price);

        var entity = new {Entity} { Id = Guid.CreateVersion7(), Name = name.Trim(), Price = price };
        entity.AddDomainEvent(DomainEvent.Create((id, ts) =>
            new {Entity}CreatedDomainEvent(entity.Id, entity.Name, id, ts)));
        return entity;
    }

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
    }
}
```

Notes: setters are `private set`; `TenantId`/audit/soft-delete members are settable by the framework
(interceptor + Finbuckle) so they aren't `private set`. Use `Guid.CreateVersion7()`, never `Guid.NewGuid()`.

## Domain event — inherit `DomainEvent` (abstract record)

```csharp
public sealed record {Entity}CreatedDomainEvent(
    Guid {Entity}Id, string Name, Guid EventId, DateTimeOffset OccurredOnUtc)
    : DomainEvent(EventId, OccurredOnUtc);
```

Raise with the `DomainEvent.Create((id, ts) => …)` helper + `AddDomainEvent(...)` (not `QueueDomainEvent`).

## EF configuration

```csharp
public sealed class {Entity}Configuration : IEntityTypeConfiguration<{Entity}>
{
    public void Configure(EntityTypeBuilder<{Entity}> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("{Entities}");                       // schema is set once on the DbContext
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);

        // soft-deletable unique field → filter on live rows only
        builder.HasIndex(x => x.Name).IsUnique().HasFilter("\"IsDeleted\" = FALSE");

        // owned value object
        builder.OwnsOne(x => x.Price, m =>
        {
            m.Property(p => p.Amount).HasColumnName("PriceAmount").HasPrecision(18, 4);
            m.Property(p => p.Currency).HasColumnName("PriceCurrency").HasMaxLength(3);
        });

        builder.Ignore(x => x.DomainEvents);
    }
}
```

- **Do NOT add a manual `HasQueryFilter` for soft-delete or tenant** — `BaseDbContext` applies both automatically.
- A child entity reached only via a parent nav-collection needs `builder.Property(x => x.Id).ValueGeneratedNever()` in **its** config, or EF inserts it as `Modified` → 0-row UPDATE. See `database.md`.

## Register in the module DbContext

Add a `DbSet`; configurations are picked up by `ApplyConfigurationsFromAssembly`:

```csharp
public DbSet<{Entity}> {Entities} => Set<{Entity}>();
```

The DbContext already extends `BaseDbContext` and calls `base.OnModelCreating` **last** — don't change that.

## Migration

Use the **create-migration** skill (build first, correct `--context`):

```bash
dotnet ef migrations add Add{Entity} \
  --project src/Host/FSH.Starter.Migrations.PostgreSQL \
  --startup-project src/Host/FSH.Starter.Api \
  --context {X}DbContext
```

## Checklist

- [ ] `sealed`, `AggregateRoot<Guid>` (+ `IHasTenant`/`IAuditableEntity`/`ISoftDeletable` as needed), private ctor, static `Create` using `Guid.CreateVersion7()`
- [ ] Domain event inherits `DomainEvent`; raised via `DomainEvent.Create` + `AddDomainEvent`
- [ ] EF config: no manual soft-delete/tenant filter; `ValueGeneratedNever()` on nav-collection children
- [ ] `DbSet` added; build green; migration created with `--context {X}DbContext`
