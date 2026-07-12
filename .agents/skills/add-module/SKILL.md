---
name: add-module
description: Create a new module (bounded context) — runtime + Contracts projects, IModule, DbContext, permissions, migrations, and the four registration sites. Use when adding a distinct business domain. For a feature in an existing module, use add-feature.
argument-hint: "[ModuleName]"
---

# Add Module

High-ceremony. The part people get wrong is **registration — a module must be wired in FOUR places**
(see Step 6). Architecture rules: `.agents/rules/architecture.md`.

## Projects

```
src/Modules/{Name}/
├── Modules.{Name}/            ← runtime (internal): Domain/, Data/, Features/v1/, {Name}Module.cs
└── Modules.{Name}.Contracts/  ← public API: v1/ (commands/queries), Dtos/, Authorization/, Events/
```

**Copy an existing module's two `.csproj` files** (e.g. `Modules.Catalog`) and rename — don't hand-write
project references. The runtime project references its Contracts project + the BuildingBlocks it needs;
the Contracts project references `Mediator` + shared contracts.

## Step 1 — `[FshModule]` is an ASSEMBLY attribute (not class-level)

In `{Name}Module.cs`, above the namespace:

```csharp
[assembly: FshModule(typeof(FSH.Modules.{Name}.{Name}Module), 900)]   // (Type, order)

namespace FSH.Modules.{Name};

public sealed class {Name}Module : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        PermissionConstants.Register({Name}Permissions.All);
        builder.Services.AddHeroDbContext<{Name}DbContext>();
        builder.Services.AddScoped<IDbInitializer, {Name}DbInitializer>();

        // Only if the module publishes/handles integration events:
        // builder.Services.AddEventingCore(builder.Configuration);
        // builder.Services.AddEventingForDbContext<{Name}DbContext>();
        // builder.Services.AddIntegrationEventHandlers(typeof({Name}Module).Assembly);

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<{Name}DbContext>(name: "db:{name}");
    }

    public void ConfigureMiddleware(IApplicationBuilder app) { }   // optional, runs after auth

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        var versionSet = endpoints.NewApiVersionSet().HasApiVersion(new ApiVersion(1)).ReportApiVersions().Build();
        var group = endpoints.MapGroup("api/v{version:apiVersion}/{name}")
            .WithTags("{Name}").WithApiVersionSet(versionSet).RequireAuthorization();
        // group.MapCreate{Entity}Endpoint();  …
    }
}
```

`Order` controls load sequence (Auditing 300, Files 350, Webhooks 400, Billing 500, Catalog 600, Tickets 700, Notifications 750, Chat 800). If your module consumes another's events, load after it.

## Step 2 — Permissions (Contracts/Authorization)

`{Name}Permissions` with nested resource classes and an `All` collection registered via `PermissionConstants.Register({Name}Permissions.All)`. Mirror the shape of `CatalogPermissions`.

## Step 3 — DbContext (extends `BaseDbContext`)

```csharp
public sealed class {Name}DbContext : BaseDbContext
{
    public const string Schema = "{name}";

    public {Name}DbContext(
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        DbContextOptions<{Name}DbContext> options,
        IOptions<DatabaseOptions> settings,
        IHostEnvironment environment)
        : base(multiTenantContextAccessor, options, settings, environment) { }

    public DbSet<{Entity}> {Entities} => Set<{Entity}>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof({Name}DbContext).Assembly);
        base.OnModelCreating(modelBuilder);   // MUST be last — applies tenant + soft-delete filters
    }
}
```

## Step 4 — Solution + project references

```bash
dotnet sln src/FSH.Starter.slnx add src/Modules/{Name}/Modules.{Name}/Modules.{Name}.csproj
dotnet sln src/FSH.Starter.slnx add src/Modules/{Name}/Modules.{Name}.Contracts/Modules.{Name}.Contracts.csproj
```

Add a `<ProjectReference>` to the runtime module from **both** `FSH.Starter.Api` and `FSH.Starter.DbMigrator`, and reference the runtime project from `FSH.Starter.Migrations.PostgreSQL`.

## Step 5 — Migrations folder

Add a `{Name}/` folder in `src/Host/FSH.Starter.Migrations.PostgreSQL`, then create the initial migration (see **create-migration**) with `--context {Name}DbContext`.

## Step 6 — ⚠️ Register in ALL FOUR places (the footgun)

Identical edits in **both** `FSH.Starter.Api/Program.cs` **and** `FSH.Starter.DbMigrator/Program.cs`:

1. Mediator `o.Assemblies` — add **two** markers: a Contracts type (e.g. `typeof(FSH.Modules.{Name}.Contracts.{Name}ContractsMarker)`) **and** the module type (`typeof({Name}Module)`).
2. `moduleAssemblies` array — add `typeof({Name}Module).Assembly`.

Miss the Mediator marker → handlers silently undiscovered. Miss the assembly entry → module never loads. Miss the DbMigrator pair → migrate/seed skips the module.

## Step 7 — Verify

```bash
dotnet build src/FSH.Starter.slnx                  # 0 warnings
dotnet test src/Tests/Architecture.Tests           # boundary + tenant-isolation rules must pass
dotnet test src/FSH.Starter.slnx
```

## Checklist

- [ ] Two projects (copied csproj), added to `.slnx`, referenced from Api + DbMigrator (+ Migrations)
- [ ] `[assembly: FshModule(typeof({Name}Module), order)]` (assembly-level, positional)
- [ ] `IModule`: `AddHeroDbContext<T>()`, `PermissionConstants.Register`, version-set group, eventing trio if needed
- [ ] `{Name}DbContext : BaseDbContext`, 4-arg ctor, `base.OnModelCreating` last
- [ ] `{Name}Permissions` in Contracts/Authorization
- [ ] Migrations folder + initial migration (`--context {Name}DbContext`)
- [ ] **Registered in all four places** (Api + DbMigrator × Mediator + moduleAssemblies)
- [ ] Build + Architecture.Tests green
