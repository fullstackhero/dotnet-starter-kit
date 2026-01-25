---
name: add-module
description: Create a new module (bounded context) with proper project structure, permissions, DbContext, and registration. Use when adding a new business domain that needs its own entities and endpoints.
argument-hint: [ModuleName]
---

# Add Module

Create a new bounded context with full project structure.

## When to Create a New Module

- Has its own domain entities
- Could be deployed independently
- Represents a distinct business domain

If it's just a feature in an existing domain, use `add-feature` instead.

## Project Structure

```
src/Modules/{Name}/
├── Modules.{Name}/
│   ├── Modules.{Name}.csproj
│   ├── {Name}Module.cs
│   ├── {Name}PermissionConstants.cs
│   ├── {Name}DbContext.cs
│   ├── Domain/
│   │   └── {Entity}.cs
│   └── Features/v1/
│       └── {Feature}/
└── Modules.{Name}.Contracts/
    ├── Modules.{Name}.Contracts.csproj
    └── DTOs/
```

## Step 1: Create Projects

### Main Module Project
`src/Modules/{Name}/Modules.{Name}/Modules.{Name}.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\BuildingBlocks\Core\Core.csproj" />
    <ProjectReference Include="..\..\BuildingBlocks\Persistence\Persistence.csproj" />
    <ProjectReference Include="..\..\BuildingBlocks\Web\Web.csproj" />
    <ProjectReference Include="..\Modules.{Name}.Contracts\Modules.{Name}.Contracts.csproj" />
  </ItemGroup>
</Project>
```

### Contracts Project
`src/Modules/{Name}/Modules.{Name}.Contracts/Modules.{Name}.Contracts.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
```

## Step 2: Implement IModule

```csharp
public sealed class {Name}Module : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        // Register DbContext
        builder.Services.AddDbContext<{Name}DbContext>((sp, options) =>
        {
            var dbOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.UseNpgsql(dbOptions.ConnectionString);
        });

        // Register repositories
        builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        builder.Services.AddScoped(typeof(IReadRepository<>), typeof(Repository<>));
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/{name}");
        // Map feature endpoints here
    }
}
```

## Step 3: Add Permission Constants

```csharp
public static class {Name}PermissionConstants
{
    public static class {Entities}
    {
        public const string View = "{Entities}.View";
        public const string Create = "{Entities}.Create";
        public const string Update = "{Entities}.Update";
        public const string Delete = "{Entities}.Delete";
    }
}
```

## Step 4: Create DbContext

```csharp
public sealed class {Name}DbContext : DbContext
{
    public {Name}DbContext(DbContextOptions<{Name}DbContext> options) : base(options) { }

    public DbSet<{Entity}> {Entities} => Set<{Entity}>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("{name}");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof({Name}DbContext).Assembly);
    }
}
```

## Step 5: Register in Program.cs

```csharp
// Add to moduleAssemblies array
var moduleAssemblies = new Assembly[]
{
    typeof(IdentityModule).Assembly,
    typeof(MultitenancyModule).Assembly,
    typeof(AuditingModule).Assembly,
    typeof({Name}Module).Assembly,  // Add here
};

// Add Mediator assemblies if module has commands/queries
builder.Services.AddMediator(o =>
{
    o.Assemblies = [
        // ... existing
        typeof({Name}Module).Assembly,
    ];
});
```

## Step 6: Add to Solution

```bash
dotnet sln src/FSH.Framework.slnx add src/Modules/{Name}/Modules.{Name}/Modules.{Name}.csproj
dotnet sln src/FSH.Framework.slnx add src/Modules/{Name}/Modules.{Name}.Contracts/Modules.{Name}.Contracts.csproj
```

## Step 7: Reference from API

In `src/Playground/Playground.Api/Playground.Api.csproj`:
```xml
<ProjectReference Include="..\..\Modules\{Name}\Modules.{Name}\Modules.{Name}.csproj" />
```

## Step 8: Verify

```bash
dotnet build src/FSH.Framework.slnx  # Must be 0 warnings
dotnet test src/FSH.Framework.slnx
```

## Checklist

- [ ] Both projects created (main + contracts)
- [ ] IModule implemented with ConfigureServices and MapEndpoints
- [ ] Permission constants defined
- [ ] DbContext created with proper schema
- [ ] Registered in Program.cs moduleAssemblies
- [ ] Added to solution file
- [ ] Referenced from Playground.Api
- [ ] Build passes with 0 warnings
