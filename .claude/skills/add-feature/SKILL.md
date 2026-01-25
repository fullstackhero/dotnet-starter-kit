---
name: add-feature
description: Create a new API endpoint with Command, Handler, Validator, and Endpoint following FSH vertical slice architecture. Use when adding any new feature, API endpoint, or business operation.
argument-hint: [ModuleName] [FeatureName]
---

# Add Feature

Create a complete vertical slice feature with all required files.

## File Structure

```
src/Modules/{Module}/Features/v1/{FeatureName}/
├── {Action}{Entity}Command.cs      # or Get{Entity}Query.cs
├── {Action}{Entity}Handler.cs
├── {Action}{Entity}Validator.cs    # Commands only
└── {Action}{Entity}Endpoint.cs
```

## Step 1: Create Command or Query

**For state changes (POST/PUT/DELETE):**
```csharp
public sealed record Create{Entity}Command(
    string Name,
    decimal Price) : ICommand<Create{Entity}Response>;
```

**For reads (GET):**
```csharp
public sealed record Get{Entity}Query(Guid Id) : IQuery<{Entity}Dto>;
```

## Step 2: Create Handler

```csharp
public sealed class Create{Entity}Handler(
    IRepository<{Entity}> repository,
    ICurrentUser currentUser) : ICommandHandler<Create{Entity}Command, Create{Entity}Response>
{
    public async ValueTask<Create{Entity}Response> Handle(
        Create{Entity}Command command,
        CancellationToken ct)
    {
        var entity = {Entity}.Create(command.Name, command.Price, currentUser.TenantId);
        await repository.AddAsync(entity, ct);
        return new Create{Entity}Response(entity.Id);
    }
}
```

## Step 3: Create Validator (Commands Only)

```csharp
public sealed class Create{Entity}Validator : AbstractValidator<Create{Entity}Command>
{
    public Create{Entity}Validator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThan(0);
    }
}
```

## Step 4: Create Endpoint

```csharp
public static class Create{Entity}Endpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", async (
            Create{Entity}Command command,
            IMediator mediator,
            CancellationToken ct) => TypedResults.Created(
                $"/{entities}/{(await mediator.Send(command, ct)).Id}"))
        .WithName(nameof(Create{Entity}Command))
        .WithSummary("Create a new {entity}")
        .RequirePermission({Module}Permissions.{Entities}.Create);
}
```

## Step 5: Add DTOs to Contracts

In `src/Modules/{Module}/Modules.{Module}.Contracts/`:

```csharp
public sealed record Create{Entity}Response(Guid Id);
public sealed record {Entity}Dto(Guid Id, string Name, decimal Price);
```

## Step 6: Wire Endpoint in Module

In `{Module}Module.cs` MapEndpoints method:

```csharp
var entities = endpoints.MapGroup("/{entities}").WithTags("{Entities}");
entities.Map{Action}{Entity}Endpoint();
```

## Step 7: Verify

```bash
dotnet build src/FSH.Framework.slnx  # Must be 0 warnings
dotnet test src/FSH.Framework.slnx
```

## Checklist

- [ ] Command/Query uses `ICommand<T>` or `IQuery<T>` (NOT MediatR's IRequest)
- [ ] Handler uses `ICommandHandler<T,R>` or `IQueryHandler<T,R>`
- [ ] Validator exists for commands
- [ ] Endpoint has `.RequirePermission()` or `.AllowAnonymous()`
- [ ] Endpoint has `.WithName()` and `.WithSummary()`
- [ ] DTOs in Contracts project, not internal
- [ ] Build passes with 0 warnings
