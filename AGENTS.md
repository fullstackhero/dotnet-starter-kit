# AGENTS.md — FullStackHero .NET 9 Starter Kit

A .NET 9 Clean Architecture monorepo (Web API + Blazor WASM + Aspire orchestrator) with Multi-Tenancy. All source under `src/`.

## Dev commands

Root solution: `src/FSH.Starter.sln`

```bash
# Restore/build/test (uses CI paths -- no test projects exist yet)
dotnet restore src/api/server/Server.csproj
dotnet build src/api/server/Server.csproj --no-restore
dotnet test  src/api/server/Server.csproj --no-build --verbosity normal

# Blazor (same pattern)
dotnet build src/apps/blazor/client/Client.csproj --no-restore
```

```bash
# Run locally
dotnet run --project src/aspire/Host/Host.csproj      # Aspire dashboard (default workflow)
dotnet run --project src/api/server/Server.csproj      # API standalone

# URLs: Aspire https://localhost:7200/ | API https://localhost:7000/swagger | Blazor https://localhost:7100/
```

```bash
# EF Core migrations (run from src/api/server, specify --context and --project)
dotnet ef migrations add "Name" --project ../migrations/postgresql/ --context TenantDbContext -o Tenant
# Contexts: IdentityDbContext, TenantDbContext, TodoDbContext, CatalogDbContext
```

```bash
# NSwag client regeneration (Blazor -> generated ApiClient.cs)
dotnet build -t:NSwag src/apps/blazor/infrastructure/Infrastructure.csproj
# Or manually run: src/apps/blazor/scripts/nswag-regen.ps1
```

```bash
# Container publish (API uses built-in container support, not Dockerfile)
dotnet publish src/api/server/Server.csproj -c Release -p:ContainerRepository=ghcr.io/<owner>/webapi -p:RuntimeIdentifier=linux-x64

# Blazor uses Dockerfile
docker build -f src/Dockerfile.Blazor -t <image> .
```

## Key architecture facts

- **Framework initialization** (see `src/api/server/Program.cs`):
  `builder.ConfigureFshFramework()` → `builder.RegisterModules()` → `app.UseFshFramework()` → `app.UseModules()`
- **Modules**: Carter-based minimal API endpoints with versioning (v1, v2). Current modules: Catalog, Todo.
- **Entrypoints**: `src/api/server/Program.cs` (API), `src/apps/blazor/client/Program.cs` (Blazor), `src/aspire/Host/Program.cs` (Aspire)
- **No Node.js/package.json** -- pure .NET. The only JS is the Blazor service worker.
- **No test projects exist** -- `dotnet test` runs but there are no test projects yet.
- **No pre-commit hooks or JS lint tooling** -- only `.editorconfig` for C# code style.
- **Central package management**: all dependency versions in `src/Directory.Packages.props`. All projects target `net9.0`.

## CI workflows (`.github/workflows/`)

| Workflow | Trigger path | Action |
|----------|-------------|--------|
| `webapi.yml` | `src/api/**` | Build, test, publish Docker image to GHCR |
| `blazor.yml` | `src/apps/blazor/**`, `Dockerfile.Blazor` | Build, test, publish to GHCR |
| `nuget.yml` | `FSH.StarterKit.nuspec` | Pack & push NuGet template package |
| `changelog.yml` | (Release Drafter) | Auto-generate release notes |

## Stack

- **Framework**: ASP.NET Core 9, Blazor WASM, EF Core 9
- **Patterns**: Clean Architecture, CQRS (MediatR), vertical slice modules, Carter endpoints
- **Key packages**: Finbuckle.MultiTenant, FluentValidation, Mapster, Ardalis.Specification, Hangfire, Serilog, OpenTelemetry, MudBlazor
- **Database**: PostgreSQL (default), MSSQL migration project also exists
- **Infrastructure**: Redis (caching), Hangfire (background jobs), MailKit (email)

## Naming conventions (from `.editorconfig`)

PascalCase: types, namespaces, methods, properties, events, public fields. `_camelCase` private fields, `s_camelCase` private static. File-scoped namespaces, top-level statements, primary constructors preferred.

## What's pending (per README)

Identity endpoints, File Storage Service, NuGet generation pipeline, source code generation, searching/sorting.

## Custom
- 1 file per 1 class