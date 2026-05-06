# FSH.Starter

Built with [FullStackHero .NET Starter Kit](https://github.com/fullstackhero/dotnet-starter-kit) — a production-ready modular .NET 10 framework.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://www.docker.com/) (for PostgreSQL, Redis, and Aspire)
- .NET Aspire workload: `dotnet workload install aspire`

## Quick Start

```bash
# Start everything with Aspire (recommended)
dotnet run --project src/Host/FSH.Starter.AppHost

# Or run the API standalone (requires external Postgres + Redis)
dotnet run --project src/Host/FSH.Starter.Api
```

The Aspire dashboard opens at `https://localhost:15888`. The API serves at `https://localhost:7030` with Scalar docs at `/scalar`.

## Project Structure

```
src/
  BuildingBlocks/       # Shared framework libraries (do not modify unless necessary)
  Modules/              # Bounded contexts (Identity, Multitenancy, Auditing, Webhooks)
  Host/
    FSH.Starter.Api/            # API host
    FSH.Starter.AppHost/        # .NET Aspire orchestrator
FSH.Starter.Migrations.PostgreSQL/  # EF Core migrations
  Tests/                # Unit, integration, and architecture tests
```

## Adding Your First Feature

1. Define command/query in `src/Modules/{Module}.Contracts/v1/{Area}/{Feature}/`
2. Add handler in `src/Modules/{Module}/Features/v1/{Area}/{Feature}/`
3. Add FluentValidation validator in the same folder
4. Add endpoint in the same folder
5. Wire the endpoint in the module's `MapEndpoints()` method

## Removing Unwanted Modules

To remove a module (e.g., Webhooks):

1. Delete `src/Modules/Webhooks/` folders
2. Remove its references from `src/Host/FSH.Starter.Api/FSH.Starter.Api.csproj`
3. Remove its assembly from `Program.cs` (both `AddMediator` and `moduleAssemblies`)
4. Remove its migration folder from `src/Host/FSH.Starter.Migrations.PostgreSQL/`
5. Remove from `src/FSH.Starter.slnx`

## Running Tests

```bash
dotnet test src/FSH.Starter.slnx
```

## Learn More

- [FullStackHero Documentation](https://fullstackhero.net)
- [GitHub Repository](https://github.com/fullstackhero/dotnet-starter-kit)
