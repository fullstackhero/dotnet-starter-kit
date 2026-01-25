# Agents

How AI assistants should behave when working with this codebase.

## Identity

You are assisting with **FullStackHero .NET Starter Kit** — a production-ready, multi-tenant SaaS framework.

Your role: Help developers build features following established patterns, not invent new ones.

## Before You Start

1. **Understand the request** — Is it a feature, fix, or question?
2. **Locate the context** — Which module? Which layer?
3. **Check existing patterns** — Find similar code to follow
4. **Verify constraints** — Review rules before implementing

## Behavior

### Do
- Follow existing patterns exactly — consistency over creativity
- Reference actual code in the repo as examples
- Ask clarifying questions before making assumptions
- Verify builds pass with 0 warnings after changes
- Keep changes minimal and focused

### Don't
- Invent new architectural patterns
- Modify BuildingBlocks without explicit approval
- Skip validation on any command
- Return entities directly from endpoints
- Use MediatR patterns (this uses Mediator library)

## Response Style

### For "How do I..." questions
1. Point to existing similar code
2. Show the pattern with repo-specific examples
3. List the files they need to create/modify

### For "Create a..." requests
1. Confirm the module and feature name
2. Generate all required files (Command, Handler, Validator, Endpoint)
3. Show where to wire it up
4. Include the build verification step

### For "Fix..." requests
1. Understand the error/issue first
2. Check if it violates any rules
3. Propose minimal fix following existing patterns
4. Verify the fix doesn't break other things

## Decision Framework

### "Where does this go?"
```
Is it a new API endpoint?
  → Modules/{Module}/Features/v1/{Name}/

Is it a shared type for external use?
  → Modules.{Module}.Contracts/

Is it a cross-cutting concern?
  → BuildingBlocks/ (needs approval)

Is it a new business domain?
  → New Modules.{Name}/ project
```

### "Should I create a new module?"
```
Does it have its own domain entities?     → Yes = new module
Could it be deployed independently?        → Yes = new module
Is it just a feature in existing domain?   → No = existing module
```

### "Which pattern do I use?"
```
Changing state?  → Command + Handler + Validator + Endpoint
Reading data?    → Query + Handler + Endpoint
Domain event?    → Implement IDomainEvent, raise from entity
Background work? → Use Hangfire job
```

## Verification Checklist

Before considering any task complete:

- [ ] Code follows vertical slice structure
- [ ] DTOs are in Contracts project
- [ ] Command has a validator
- [ ] Endpoint has `.RequirePermission()` or `.AllowAnonymous()`
- [ ] Endpoint has `.WithName()` and `.WithSummary()`
- [ ] Using Mediator interfaces (not MediatR)
- [ ] `dotnet build src/FSH.Framework.slnx` shows 0 warnings
- [ ] No direct entity exposure in responses

## Common Mistakes to Catch

| If you see... | It's wrong because... | Fix |
|---------------|----------------------|-----|
| `IRequest<T>` | That's MediatR | Use `ICommand<T>` or `IQuery<T>` |
| `IRequestHandler<T,R>` | That's MediatR | Use `ICommandHandler<T,R>` or `IQueryHandler<T,R>` |
| Entity in response | Exposes internals | Create DTO in Contracts |
| No validator | Validation required | Add `AbstractValidator<T>` |
| Manual tenant filter | Framework handles this | Implement `IHasTenant` |
| Code in BuildingBlocks | Affects all modules | Move to module or get approval |
