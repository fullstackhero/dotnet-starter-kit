# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
# Restore and build
dotnet restore src/FSH.Framework.slnx
dotnet build src/FSH.Framework.slnx

# Run with Aspire (spins up Postgres + Redis via Docker)
dotnet run --project src/Playground/FSH.Playground.AppHost

# Run API standalone (requires DB/Redis/JWT config in appsettings)
dotnet run --project src/Playground/Playground.Api

# Run all tests
dotnet test src/FSH.Framework.slnx

# Run single test project
dotnet test src/Tests/Architecture.Tests

# Run specific test
dotnet test src/Tests/Architecture.Tests --filter "FullyQualifiedName~TestMethodName"

# Generate C# API client from OpenAPI spec (requires API running)
./scripts/openapi/generate-api-clients.ps1 -SpecUrl "https://localhost:7030/openapi/v1.json"

# Check for OpenAPI drift (CI validation)
./scripts/openapi/check-openapi-drift.ps1 -SpecUrl "<spec-url>"
```

## Architecture

FullStackHero .NET 10 Starter Kit - multi-tenant SaaS framework using vertical slice architecture.

### Repository Structure

- **src/BuildingBlocks/** - Reusable framework components (packaged as NuGets): Core (DDD primitives), Persistence (EF Core + specifications), Caching (Redis), Mailing, Jobs (Hangfire), Storage, Web (host wiring), Eventing
- **src/Modules/** - Feature modules (packaged as NuGets): Identity (JWT auth, users, roles), Multitenancy (Finbuckle), Auditing
- **src/Playground/** - Reference implementation using direct project references for development; includes Aspire AppHost, API, Blazor UI, PostgreSQL migrations
- **src/Tests/** - Architecture tests using NetArchTest.Rules, xUnit, Shouldly
- **scripts/openapi/** - NSwag-based C# client generation from OpenAPI spec; outputs to `Playground.Blazor/ApiClient/Generated.cs`
- **terraform/** - AWS infrastructure as code (modular)
  - `modules/` - Reusable: network, ecs_cluster, ecs_service, rds_postgres, elasticache_redis, alb, s3_bucket
  - `apps/playground/` - Playground deployment stack with `envs/{dev,staging,prod}/{region}/`
  - `bootstrap/` - Initial AWS setup (S3 backend, etc.)

### Module Pattern

Each module implements `IModule` with:
- `ConfigureServices(IHostApplicationBuilder)` - DI registration
- `MapEndpoints(IEndpointRouteBuilder)` - Minimal API endpoint mapping

Feature structure within modules:
```
Features/v1/{Feature}/
├── {Feature}Command.cs (or Query)
├── {Feature}Handler.cs
├── {Feature}Validator.cs (FluentValidation)
└── {Feature}Endpoint.cs (static extension method on IEndpointRouteBuilder)
```

Contracts projects (`Modules.{Name}.Contracts/`) contain public DTOs shareable with clients.

### Endpoint Pattern

Endpoints are static extension methods returning `RouteHandlerBuilder`:
```csharp
public static RouteHandlerBuilder MapXxxEndpoint(this IEndpointRouteBuilder endpoint)
{
    return endpoint.MapPost("/path", async (..., IMediator mediator, CancellationToken ct) =>
    {
        var result = await mediator.Send(command, ct);
        return TypedResults.Ok(result);
    });
}
```

### Platform Wiring

In `Program.cs`:
1. Register Mediator with command/query assemblies
2. Call `builder.AddHeroPlatform(...)` - enables auth, OpenAPI, caching, mailing, jobs, health, OTel
3. Call `builder.AddModules(moduleAssemblies)` to load modules
4. Call `app.UseHeroMultiTenantDatabases()` for tenant DB migrations
5. Call `app.UseHeroPlatform(p => p.MapModules = true)` to wire endpoints

## Configuration

Key settings (appsettings or env vars):
- `DatabaseOptions:Provider` - postgres or mssql
- `DatabaseOptions:ConnectionString` - Primary database
- `CachingOptions:Redis` - Redis connection
- `JwtOptions:SigningKey` - Required in production

## Code Standards

- .NET 10, C# latest, nullable enabled
- SonarAnalyzer.CSharp with code style enforced in build
- API versioning in URL path (`/api/v1/...`)
- Mediator library (not MediatR) for commands/queries
- FluentValidation for request validation
