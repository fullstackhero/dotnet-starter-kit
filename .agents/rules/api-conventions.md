---
paths:
  - "src/Modules/**/Features/**/*"
  - "src/Modules/**/*Endpoint*.cs"
---

# API Conventions

Rules for API endpoints in FSH.

## Endpoint Requirements

Every endpoint MUST have:

```csharp
endpoints.MapPost("/", handler)
    .WithName(nameof(CommandOrQuery))      // Required: Unique name
    .WithSummary("Description")             // Required: OpenAPI description
    .RequirePermission(Permission)          // Required: Or .AllowAnonymous()
```

## HTTP Method Mapping

| Operation | Method | Return |
|-----------|--------|--------|
| Create | `MapPost` | `TypedResults.Created(...)` |
| Read single | `MapGet` | `TypedResults.Ok(...)` |
| Read list | `MapGet` | `TypedResults.Ok(...)` |
| Update | `MapPut` | `TypedResults.Ok(...)` or `NoContent()` |
| Delete | `MapDelete` | `TypedResults.NoContent()` |

## Route Patterns

```
/api/v1/{module}/{entities}           # Collection
/api/v1/{module}/{entities}/{id}      # Single item
/api/v1/{module}/{entities}/{id}/sub  # Sub-resource
```

## Response Types

Always use `TypedResults`:
- `TypedResults.Ok(data)`
- `TypedResults.Created($"/path/{id}", data)`
- `TypedResults.NoContent()`
- `TypedResults.NotFound()`
- `TypedResults.BadRequest(errors)`

Never return raw objects or use `Results.Ok()`.

## Permission Format

```csharp
.RequirePermission({Module}Permissions.{Entity}.{Action})
```

Actions: `View`, `Create`, `Update`, `Delete`

## Query Parameters

Use `[AsParameters]` for complex queries:

```csharp
endpoints.MapGet("/", async ([AsParameters] GetProductsQuery query, ...) => ...)
```
