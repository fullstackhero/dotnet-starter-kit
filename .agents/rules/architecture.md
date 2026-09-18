# Architecture rules

Modular Monolith + Vertical Slice Architecture (VSA). Read this before adding/moving modules or touching wiring.

## Layers & dependency direction

```
Host (composition root)  →  Modules.{Name} (runtime)  →  Modules.{Name}.Contracts (public API)
                         →  BuildingBlocks (shared framework)
```

- **BuildingBlocks** (`src/BuildingBlocks/`) — Core, Persistence, Web, Caching, Eventing, Storage, Quota, Jobs, Mailing, Shared. Consumed by all modules. **Do not modify without explicit approval.**
- **Modules** (`src/Modules/{Name}/`) — bounded contexts. Each = a runtime project (internal) + a `.Contracts` project (public API: commands, queries, events, DTOs, service interfaces).
- A module **MUST NOT** reference another module's runtime project — only its `.Contracts`. Enforced by `Architecture.Tests` (NetArchTest).

## Module = runtime + Contracts

```
Modules.Identity/            ← runtime (internal): handlers, services, domain, data
Modules.Identity.Contracts/  ← public: ICommand/IQuery types, DTOs, events, service interfaces
```

Cross-module communication: through Contracts service interfaces or integration events only.

## Feature folder layout (VSA)

Each feature is a vertical slice in `Features/v{version}/{Area}/{Feature}/`:

```
Features/v1/Users/RegisterUser/
├── RegisterUserEndpoint.cs          # minimal API endpoint
├── RegisterUserCommandHandler.cs    # CQRS handler (public sealed)
└── RegisterUserCommandValidator.cs  # FluentValidation
```

Module support folders: `Domain/`, `Data/`, `Services/`, `Events/`, `Authorization/`.

## IModule registration

Each module implements `IModule`, declared via an **assembly-level** `[FshModule]` attribute (positional `(Type moduleType, int order = 0)`) — **not** a class-level `[FshModule(Order = n)]`:

```csharp
[assembly: FshModule(typeof(FSH.Modules.Identity.IdentityModule), 1)]   // above the namespace

namespace FSH.Modules.Identity;

public sealed class IdentityModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder) { ... }
    public void ConfigureMiddleware(IApplicationBuilder app) { ... }   // optional, runs AFTER UseAuthentication
    public void MapEndpoints(IEndpointRouteBuilder endpoints) { ... }
}
```

`ModuleLoader.AddModules` (`src/BuildingBlocks/Web/Modules/ModuleLoader.cs`) discovers `[FshModule]` attributes, orders by `Order` then name, instantiates each, and calls `ConfigureServices`. Endpoints map under `api/v{version:apiVersion}/{module}`.

## ⚠️ The four-place registration footgun

Adding a module requires editing **four** lists. Miss one and it fails *silently*:

| Place | File | Symptom if missed |
|---|---|---|
| Mediator `o.Assemblies` (two markers: Contracts type **and** module type) | `src/Host/FSH.Starter.Api/Program.cs` | Handlers silently undiscovered |
| `moduleAssemblies` array | `src/Host/FSH.Starter.Api/Program.cs` | Module never loaded |
| Mediator assemblies (same pair) | `src/Host/FSH.Starter.DbMigrator/Program.cs` | Migrate/seed misses the module |
| module assemblies array | `src/Host/FSH.Starter.DbMigrator/Program.cs` | Migrate/seed misses the module |

After wiring, the fastest sanity check is: build, hit the endpoint, confirm the handler runs.

## DI & handler conventions

- Mediator handlers: `public sealed`, implement `ICommandHandler<T,TResponse>` / `IQueryHandler<T,TResponse>`, return `ValueTask<T>`, `.ConfigureAwait(false)` on every await. `ServiceLifetime.Scoped`.
- Validators auto-register via `ModuleLoader` (`AddValidatorsFromAssemblies`). Name them `{Command}Validator`.
- Prefer constructor injection / primary constructors. Watch DI lifetimes: stateful singletons must be thread-safe (use `ConcurrentDictionary` / immutable snapshots).

## Middleware ordering (critical)

In `src/BuildingBlocks/Web/Extensions.cs` (`UseHeroPlatform`):

1. ExceptionHandler → ResponseCompression
2. **CORS before HTTPS redirect** (so OPTIONS preflight isn't 307-redirected)
3. HttpsRedirection → SecurityHeaders → static files → Routing
4. **`UseAuthentication`**
5. **`UseHeroLocalization`** — request localization, sits **between `UseAuthentication` and `UseAuthorization`** so the user-`locale`-claim culture provider can read `HttpContext.User`
6. **`UseModuleMiddlewares`** — each module's `ConfigureMiddleware`, runs **after** auth
7. RateLimiting → Quotas → `UseAuthorization` → `MapModules`

`app.UseHeroMultiTenantDatabases()` (Finbuckle `UseMultiTenant()`) runs in `Program.cs` **before** `UseHeroPlatform`, i.e. **before `UseAuthentication`** — so tenant resolution is header-driven, not claim-driven. See `modules/multitenancy.md`.

## Static/global state

No global mutable static collections enumerated under concurrency. `Audit` (Auditing module) swaps an immutable `IAuditEnricher[]` atomically; `ModuleLoader` guards with a lock. Follow that pattern if you must hold process-global state.

## Configuration & options

- `appsettings.json` (+ `.Development`/`.Production`) live in `src/Host/FSH.Starter.Api/`. DbMigrator links the same files.
- Bind config with the Options pattern: `AddOptions<T>().BindConfiguration(nameof(T))`, section name == type name (e.g. `JwtOptions`, `DatabaseOptions`, `CachingOptions`, `CorsOptions`, `QuotaOptions`, `RateLimitingOptions`; **storage section is `Storage`**, not `StorageOptions`). Add `.ValidateDataAnnotations().ValidateOnStart()` for fail-fast.
- Validate critical options via `IValidatableObject` — `JwtOptions` requires `SigningKey` ≥32 chars and **rejects placeholder strings containing `"replace-with"`**; `DatabaseOptions` rejects empty connection strings.
- **Production fail-fast** (`Program.cs`, before service registration): missing `DatabaseOptions:ConnectionString`, `CachingOptions:Redis`, or `JwtOptions:SigningKey` throws. Dev secrets via `dotnet user-secrets` (AppHost has a `UserSecretsId`); MinIO creds are Aspire secret parameters.
- Platform composition is one call each: `builder.AddHeroPlatform(o => { o.Enable... })` (DI) and `app.UseHeroPlatform(...)` (middleware). Feature flags toggle Caching/Jobs/Mailing/Quotas/Sse/Realtime/OpenTelemetry/CORS/Idempotency.
