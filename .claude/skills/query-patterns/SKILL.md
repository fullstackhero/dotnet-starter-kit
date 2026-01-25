---
name: query-patterns
description: Query patterns including pagination, search, filtering, and specifications for FSH. Use when implementing GET endpoints that return lists or need filtering.
---

# Query Patterns

Reference for implementing queries with pagination, search, and filtering.

## Basic Paginated Query

```csharp
// Query
public sealed record Get{Entities}Query(
    string? Search,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedList<{Entity}Dto>>;

// Handler
public sealed class Get{Entities}Handler(
    IReadRepository<{Entity}> repository) : IQueryHandler<Get{Entities}Query, PagedList<{Entity}Dto>>
{
    public async ValueTask<PagedList<{Entity}Dto>> Handle(
        Get{Entities}Query query,
        CancellationToken ct)
    {
        var spec = new {Entity}SearchSpec(query.Search, query.PageNumber, query.PageSize);
        return await repository.PaginatedListAsync(spec, ct);
    }
}
```

## Specification Pattern

```csharp
public sealed class {Entity}SearchSpec : EntitiesByPaginationFilterSpec<{Entity}, {Entity}Dto>
{
    public {Entity}SearchSpec(string? search, int pageNumber, int pageSize)
        : base(new PaginationFilter(pageNumber, pageSize))
    {
        Query
            .OrderByDescending(x => x.CreatedAt)
            .Where(x => string.IsNullOrEmpty(search) ||
                        x.Name.Contains(search) ||
                        x.Description!.Contains(search));
    }
}
```

## Get Single Entity

```csharp
// Query
public sealed record Get{Entity}Query(Guid Id) : IQuery<{Entity}Dto>;

// Handler
public sealed class Get{Entity}Handler(
    IReadRepository<{Entity}> repository) : IQueryHandler<Get{Entity}Query, {Entity}Dto>
{
    public async ValueTask<{Entity}Dto> Handle(Get{Entity}Query query, CancellationToken ct)
    {
        var spec = new {Entity}ByIdSpec(query.Id);
        var entity = await repository.FirstOrDefaultAsync(spec, ct);

        return entity ?? throw new NotFoundException($"{Entity} {query.Id} not found");
    }
}

// Specification
public sealed class {Entity}ByIdSpec : Specification<{Entity}, {Entity}Dto>, ISingleResultSpecification<{Entity}>
{
    public {Entity}ByIdSpec(Guid id)
    {
        Query.Where(x => x.Id == id);
    }
}
```

## Advanced Filtering

```csharp
public sealed record Get{Entities}Query(
    string? Search,
    Guid? CategoryId,
    decimal? MinPrice,
    decimal? MaxPrice,
    DateTimeOffset? CreatedAfter,
    bool? IsActive,
    string? SortBy,
    bool SortDescending = false,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedList<{Entity}Dto>>;

public sealed class {Entity}FilterSpec : EntitiesByPaginationFilterSpec<{Entity}, {Entity}Dto>
{
    public {Entity}FilterSpec(Get{Entities}Query query)
        : base(new PaginationFilter(query.PageNumber, query.PageSize))
    {
        Query
            // Search
            .Where(x => string.IsNullOrEmpty(query.Search) ||
                        x.Name.Contains(query.Search))

            // Filters
            .Where(x => !query.CategoryId.HasValue ||
                        x.CategoryId == query.CategoryId)
            .Where(x => !query.MinPrice.HasValue ||
                        x.Price >= query.MinPrice)
            .Where(x => !query.MaxPrice.HasValue ||
                        x.Price <= query.MaxPrice)
            .Where(x => !query.CreatedAfter.HasValue ||
                        x.CreatedAt >= query.CreatedAfter)
            .Where(x => !query.IsActive.HasValue ||
                        x.IsActive == query.IsActive);

        // Dynamic sorting
        ApplySorting(query.SortBy, query.SortDescending);
    }

    private void ApplySorting(string? sortBy, bool descending)
    {
        switch (sortBy?.ToLowerInvariant())
        {
            case "name":
                if (descending) Query.OrderByDescending(x => x.Name);
                else Query.OrderBy(x => x.Name);
                break;
            case "price":
                if (descending) Query.OrderByDescending(x => x.Price);
                else Query.OrderBy(x => x.Price);
                break;
            default:
                Query.OrderByDescending(x => x.CreatedAt);
                break;
        }
    }
}
```

## Endpoint Patterns

### List Endpoint
```csharp
public static RouteHandlerBuilder MapGet{Entities}Endpoint(this IEndpointRouteBuilder endpoints) =>
    endpoints.MapGet("/", async (
        [AsParameters] Get{Entities}Query query,
        IMediator mediator,
        CancellationToken ct) => TypedResults.Ok(await mediator.Send(query, ct)))
    .WithName(nameof(Get{Entities}Query))
    .WithSummary("Get paginated list of {entities}")
    .RequirePermission({Module}Permissions.{Entities}.View);
```

### Single Entity Endpoint
```csharp
public static RouteHandlerBuilder MapGet{Entity}Endpoint(this IEndpointRouteBuilder endpoints) =>
    endpoints.MapGet("/{id:guid}", async (
        Guid id,
        IMediator mediator,
        CancellationToken ct) => TypedResults.Ok(await mediator.Send(new Get{Entity}Query(id), ct)))
    .WithName(nameof(Get{Entity}Query))
    .WithSummary("Get {entity} by ID")
    .RequirePermission({Module}Permissions.{Entities}.View);
```

## Response Types

```csharp
// In Contracts project
public sealed record {Entity}Dto(
    Guid Id,
    string Name,
    decimal Price,
    string? Description,
    DateTimeOffset CreatedAt);

// PagedList<T> is from BuildingBlocks
// Returns: Items, PageNumber, PageSize, TotalCount, TotalPages
```

## Key Points

1. **Use specifications** - Don't write raw LINQ in handlers
2. **Tenant filtering is automatic** - Framework handles `IHasTenant`
3. **Soft delete filtering is automatic** - DeletedAt != null filtered out
4. **Use `[AsParameters]`** - For query parameters in endpoints
5. **Project to DTOs** - Never return entities directly
