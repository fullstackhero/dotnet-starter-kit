![FullStackHero .NET Starter Kit](./assets/fullstackhero-dotnet-starter-kit.png)

# FullStackHero .NET 8 Starter Kit

> With ASP.NET Core & Blazor Client

FullStackHero .NET Starter Kit is a starting point for your next `.NET 8 Clean Architecture` Solution that incorporates the most essential packages and features your projects will ever need including out-of-the-box Multi-Tenancy support. This project can save well over 200+ hours of development time for your team.

# Important

This project is currently work in progress. The NuGet package is not yet available for v2. For now, you can fork this repository to try it out. [Follow @iammukeshm on X](https://x.com/iammukeshm) for project related updates.

# ✨ Features

- C# 12
- .NET 8
- ASP.NET Core
- Minimal APIs
- EF Core
- Swagger UI
- Clean Architecture Principles
- Vertical Slice Architecture
- Modular Monolith

# What's Pending?

- Few Identity Endpoints
- Blazor Client
- File Storage Service
- NuGet Generation Pipeline
- Source Code Generation
- Searching / Sorting

# Endpoints

- [x] Tenants
  - [x] Create Tenant
  - [x] Get List of Tenants
- [x] Users
  - [x] Register
  - [x] Update Profile
  - [x] Get List of Users
- [x] Token
  - [x] Generate JWT
- [x] Products
  - [x] Create
  - [x] Get
  - [x] Get By ID
  - [x] Update
  - [x] Delete
- [x] Todo
  - [x] Create
  - [x] Get
  - [x] Get By ID
  - [x] Update
  - [x] Delete

# Add Migrations

Navigate to `./api/server` and run the following EF CLI commands.

```bash
dotnet ef migrations add "Add Identity Schema" --project .././migrations/postgresql/ --context IdentityDbContext -o Identity
dotnet ef migrations add "Add Tenant Schema" --project .././migrations/postgresql/ --context TenantDbContext -o Tenant
dotnet ef migrations add "Add Todo Schema" --project .././migrations/postgresql/ --context TodoDbContext -o Todo
dotnet ef migrations add "Add Catalog Schema" --project .././migrations/postgresql/ --context CatalogDbContext -o Catalog
```
