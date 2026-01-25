---
name: mediator-reference
description: Mediator library patterns and interfaces for FSH. This project uses the Mediator source generator, NOT MediatR. Reference when implementing commands, queries, and handlers.
user-invocable: false
---

# Mediator Reference

⚠️ **FSH uses the `Mediator` source generator library, NOT `MediatR`.**

These are different libraries with different interfaces. Using MediatR interfaces will cause build errors.

## Interface Comparison

| Purpose | ✅ Mediator (Use This) | ❌ MediatR (Don't Use) |
|---------|------------------------|------------------------|
| Command | `ICommand<TResponse>` | `IRequest<TResponse>` |
| Query | `IQuery<TResponse>` | `IRequest<TResponse>` |
| Command Handler | `ICommandHandler<TCommand, TResponse>` | `IRequestHandler<TRequest, TResponse>` |
| Query Handler | `IQueryHandler<TQuery, TResponse>` | `IRequestHandler<TRequest, TResponse>` |
| Notification | `INotification` | `INotification` |
| Notification Handler | `INotificationHandler<T>` | `INotificationHandler<T>` |

## Command Pattern

```csharp
// ✅ Correct - Mediator
public sealed record CreateUserCommand(string Email, string Name) : ICommand<Guid>;

public sealed class CreateUserHandler : ICommandHandler<CreateUserCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateUserCommand command, CancellationToken ct)
    {
        // Implementation
    }
}

// ❌ Wrong - MediatR
public sealed record CreateUserCommand(string Email, string Name) : IRequest<Guid>;

public sealed class CreateUserHandler : IRequestHandler<CreateUserCommand, Guid>
{
    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken ct)
    {
        // This won't work!
    }
}
```

## Query Pattern

```csharp
// ✅ Correct - Mediator
public sealed record GetUserQuery(Guid Id) : IQuery<UserDto>;

public sealed class GetUserHandler : IQueryHandler<GetUserQuery, UserDto>
{
    public async ValueTask<UserDto> Handle(GetUserQuery query, CancellationToken ct)
    {
        // Implementation
    }
}
```

## Key Differences

| Aspect | Mediator | MediatR |
|--------|----------|---------|
| Return type | `ValueTask<T>` | `Task<T>` |
| Parameter name | `command` / `query` | `request` |
| Registration | Source generated | Runtime reflection |
| Performance | Faster (compile-time) | Slower (runtime) |

## Sending Commands/Queries

```csharp
// In endpoint or controller
public class MyEndpoint
{
    public static async Task<IResult> Handle(
        CreateUserCommand command,
        IMediator mediator,  // Same interface name
        CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return TypedResults.Created($"/users/{result}");
    }
}
```

## Registration

```csharp
// In Program.cs
builder.Services.AddMediator(options =>
{
    options.Assemblies =
    [
        typeof(IdentityModule).Assembly,
        typeof(MultitenancyModule).Assembly,
        // Add your module assemblies here
    ];
});
```

## Common Errors

### Error: `IRequest<T>` not found
**Cause:** Using MediatR interface
**Fix:** Change to `ICommand<T>` or `IQuery<T>`

### Error: `IRequestHandler<T,R>` not found
**Cause:** Using MediatR interface
**Fix:** Change to `ICommandHandler<T,R>` or `IQueryHandler<T,R>`

### Error: Handler not found at runtime
**Cause:** Assembly not registered in AddMediator
**Fix:** Add assembly to `options.Assemblies` array

### Error: `Task<T>` vs `ValueTask<T>`
**Cause:** Using MediatR return type
**Fix:** Change handler return type to `ValueTask<T>`

## Namespaces

```csharp
// ✅ Correct
using Mediator;

// ❌ Wrong
using MediatR;
```
