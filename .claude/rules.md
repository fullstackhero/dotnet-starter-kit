# Rules

Hard constraints that must always be followed.

## Architecture Rules

1. **Vertical Slices Only**
   - Every feature lives in `Modules/{Module}/Features/v1/{FeatureName}/`
   - Never scatter feature code across multiple directories
   - One folder = one complete feature

2. **Module Boundaries**
   - Modules only communicate through Contracts projects
   - Never reference internal types across modules
   - DTOs always in `Modules.{Name}.Contracts/`

3. **BuildingBlocks Protection**
   - Changes to `BuildingBlocks/` require explicit approval
   - These affect ALL modules - changes ripple everywhere
   - Prefer extension over modification

## Code Rules

4. **Mediator Not MediatR**
   - Use `Mediator` source generator library
   - Interfaces: `ICommand<T>`, `IQuery<T>`, `ICommandHandler<T,R>`, `IQueryHandler<T,R>`
   - Never use `IRequest<T>` or `IRequestHandler<T,R>` (those are MediatR)

5. **Validation Required**
   - Every command must have an `AbstractValidator<TCommand>`
   - No exceptions - validation is not optional
   - Validators auto-register via FluentValidation

6. **No Entity Exposure**
   - Never return domain entities from endpoints
   - Always map to DTOs from Contracts project
   - Entities are internal implementation details

7. **Explicit Authorization**
   - Every endpoint needs `.RequirePermission()` or `.AllowAnonymous()`
   - Use permission constants from `{Module}PermissionConstants`
   - No implicit security

8. **Zero Warnings**
   - Build must pass with 0 warnings
   - Run `dotnet build src/FSH.Framework.slnx` and verify
   - CI enforces this - no exceptions

## Naming Rules

9. **Consistent Naming**
   | Type | Pattern |
   |------|---------|
   | Commands | `{Action}{Entity}Command` |
   | Queries | `Get{Entity}Query` or `Get{Entities}Query` |
   | Handlers | `{CommandOrQuery}Handler` |
   | Validators | `{Command}Validator` |
   | Endpoints | `{CommandOrQuery}Endpoint` |
   | DTOs | `{Entity}Dto`, `{Entity}Response`, `{Action}{Entity}Request` |

10. **File = Type**
    - One public type per file
    - Filename matches type name exactly
    - `CreateUserCommand.cs` contains `CreateUserCommand`

## Multi-Tenancy Rules

11. **Tenant Isolation**
    - Entities with tenant data implement `IHasTenant`
    - Framework auto-filters queries by tenant
    - Never manually filter by TenantId in queries

12. **No Hardcoded Tenants**
    - Never hardcode tenant IDs
    - Use `ICurrentUser.TenantId` for current tenant
    - Tenant context comes from framework
