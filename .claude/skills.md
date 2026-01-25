# Skills

Step-by-step guides for common tasks.

## Skill: Add a Feature

**When:** Creating a new API endpoint with business logic.

**Steps:**

1. **Create the folder**
   ```
   Modules/{Module}/Features/v1/{FeatureName}/
   ```

2. **Create Command or Query**
   ```csharp
   // For state changes
   public sealed record CreateProductCommand(
       string Name,
       decimal Price) : ICommand<CreateProductResponse>;
   
   // For reads
   public sealed record GetProductQuery(Guid Id) : IQuery<ProductDto>;
   ```

3. **Create Handler**
   ```csharp
   public sealed class CreateProductHandler(
       IRepository<Product> repository,
       ICurrentUser currentUser) : ICommandHandler<CreateProductCommand, CreateProductResponse>
   {
       public async ValueTask<CreateProductResponse> Handle(
           CreateProductCommand command, 
           CancellationToken ct)
       {
           var product = Product.Create(command.Name, command.Price, currentUser.TenantId);
           await repository.AddAsync(product, ct);
           return new CreateProductResponse(product.Id);
       }
   }
   ```

4. **Create Validator** (commands only)
   ```csharp
   public sealed class CreateProductValidator : AbstractValidator<CreateProductCommand>
   {
       public CreateProductValidator()
       {
           RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
           RuleFor(x => x.Price).GreaterThan(0);
       }
   }
   ```

5. **Create Endpoint**
   ```csharp
   public static class CreateProductEndpoint
   {
       public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
           endpoints.MapPost("/", async (
               CreateProductCommand command,
               IMediator mediator,
               CancellationToken ct) => TypedResults.Created(
                   $"/products/{(await mediator.Send(command, ct)).Id}"))
           .WithName(nameof(CreateProductCommand))
           .WithSummary("Create a new product")
           .RequirePermission(CatalogPermissions.Products.Create);
   }
   ```

6. **Wire in Module**
   ```csharp
   // In module's MapEndpoints method
   var products = endpoints.MapGroup("/products").WithTags("Products");
   products.MapCreateProductEndpoint();
   ```

7. **Add DTO to Contracts**
   ```csharp
   // In Modules.{Module}.Contracts/
   public sealed record CreateProductResponse(Guid Id);
   public sealed record ProductDto(Guid Id, string Name, decimal Price);
   ```

8. **Verify**
   ```bash
   dotnet build src/FSH.Framework.slnx  # Must show 0 warnings
   dotnet test src/FSH.Framework.slnx
   ```

---

## Skill: Add a Module

**When:** Creating a new bounded context (new business domain).

**Steps:**

1. **Create projects**
   ```
   src/Modules/{Name}/
   ├── Modules.{Name}/
   │   ├── Modules.{Name}.csproj
   │   ├── {Name}Module.cs
   │   ├── {Name}PermissionConstants.cs
   │   └── Features/v1/
   └── Modules.{Name}.Contracts/
       └── Modules.{Name}.Contracts.csproj
   ```

2. **Implement IModule**
   ```csharp
   public sealed class CatalogModule : IModule
   {
       public void ConfigureServices(IHostApplicationBuilder builder)
       {
           // Register services, DbContext, etc.
       }

       public void MapEndpoints(IEndpointRouteBuilder endpoints)
       {
           var group = endpoints.MapGroup("/api/v1/catalog");
           // Map feature endpoints here
       }
   }
   ```

3. **Add permission constants**
   ```csharp
   public static class CatalogPermissionConstants
   {
       public static class Products
       {
           public const string View = "Products.View";
           public const string Create = "Products.Create";
           public const string Update = "Products.Update";
           public const string Delete = "Products.Delete";
       }
   }
   ```

4. **Register in Program.cs**
   ```csharp
   var moduleAssemblies = new Assembly[]
   {
       typeof(IdentityModule).Assembly,
       typeof(MultitenancyModule).Assembly,
       typeof(AuditingModule).Assembly,
       typeof(CatalogModule).Assembly,  // Add new module
   };
   ```

5. **Add Mediator assemblies** (if module has commands/queries)
   ```csharp
   builder.Services.AddMediator(o =>
   {
       o.Assemblies = [
           // ... existing assemblies
           typeof(CreateProductCommand).Assembly,
           typeof(CreateProductHandler).Assembly,
       ];
   });
   ```

---

## Skill: Add Entity with Multi-Tenancy

**When:** Creating a domain entity that should be tenant-isolated.

```csharp
public sealed class Product : AggregateRoot<Guid>, IHasTenant, IAuditableEntity, ISoftDeletable
{
    public string Name { get; private set; } = null!;
    public decimal Price { get; private set; }
    
    // IHasTenant
    public string TenantId { get; private set; } = null!;
    
    // IAuditableEntity
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedAt { get; set; }
    public string? LastModifiedBy { get; set; }
    
    // ISoftDeletable
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    
    private Product() { } // EF Core
    
    public static Product Create(string name, decimal price, string tenantId)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Price = price,
            TenantId = tenantId
        };
        product.AddDomainEvent(new ProductCreatedEvent(product.Id));
        return product;
    }
}
```

---

## Skill: Query with Pagination

```csharp
public sealed record GetProductsQuery(
    string? Search,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedList<ProductDto>>;

public sealed class GetProductsHandler(
    IReadRepository<Product> repository) : IQueryHandler<GetProductsQuery, PagedList<ProductDto>>
{
    public async ValueTask<PagedList<ProductDto>> Handle(
        GetProductsQuery query, 
        CancellationToken ct)
    {
        var spec = new ProductSearchSpec(query.Search, query.PageNumber, query.PageSize);
        return await repository.PaginatedListAsync(spec, ct);
    }
}
```

---

## Skill: Run & Test

```bash
# Build (must be 0 warnings)
dotnet build src/FSH.Framework.slnx

# Run tests
dotnet test src/FSH.Framework.slnx

# Run with Aspire (Postgres + Redis auto-provisioned)
dotnet run --project src/Playground/FSH.Playground.AppHost

# Run API only (manual DB setup required)
dotnet run --project src/Playground/Playground.Api

# Add migration
dotnet ef migrations add {Name} \
  --project src/Playground/Migrations.PostgreSQL \
  --startup-project src/Playground/Playground.Api

# Apply migrations
dotnet ef database update \
  --project src/Playground/Migrations.PostgreSQL \
  --startup-project src/Playground/Playground.Api
```
