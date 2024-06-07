# FullStackHero .NET 8 Starter Kit 🚀

> With ASP.NET Core Web API & Blazor Client

FullStackHero .NET Starter Kit is a starting point for your next `.NET 8 Clean Architecture` Solution that incorporates the most essential packages and features your projects will ever need including out-of-the-box Multi-Tenancy support. This project can save well over 200+ hours of development time for your team.

![FullStackHero .NET Starter Kit](./assets/fullstackhero-dotnet-starter-kit.png)

# Important

This project is currently work in progress. The NuGet package is not yet available for v2. For now, you can fork this repository to try it out. [Follow @iammukeshm on X](https://x.com/iammukeshm) for project related updates.

# 🔎 The Project

# ✨ Technologies

- ASP.NET Core 8
- Entity Framework Core 8
- Blazor
- MediatR
- PostgreSQL
- Redis
- FluentValidation

# 👨‍🚀 Architecture

# 📬 Service Endpoints

| Endpoint | Method | Description      |
| -------- | ------ | ---------------- |
| `/token` | POST   | Generates Token. |

# 🧪 Running Locally

# 🐳 Docker Support

# ☁️ Deploying to AWS

# 🤝 Contributing

# 🍕 Community

Thanks to the community who contribute to this repository! [Submit your PR and join the elite list!](CONTRIBUTING.md)

[![FullStackHero .NET Starter Kit Contributors](https://contrib.rocks/image?repo=fullstackhero/dotnet-starter-kit "FullStackHero .NET Starter Kit Contributors")](https://github.com/fullstackhero/dotnet-starter-kit/graphs/contributors)

# 📝 Notes

## Add Migrations

Navigate to `./api/server` and run the following EF CLI commands.

```bash
dotnet ef migrations add "Add Identity Schema" --project .././migrations/postgresql/ --context IdentityDbContext -o Identity
dotnet ef migrations add "Add Tenant Schema" --project .././migrations/postgresql/ --context TenantDbContext -o Tenant
dotnet ef migrations add "Add Todo Schema" --project .././migrations/postgresql/ --context TodoDbContext -o Todo
dotnet ef migrations add "Add Catalog Schema" --project .././migrations/postgresql/ --context CatalogDbContext -o Catalog
```

## What's Pending?

- Few Identity Endpoints
- Blazor Client
- File Storage Service
- NuGet Generation Pipeline
- Source Code Generation
- Searching / Sorting

# ⚖️ LICENSE

MIT © [fullstackhero](LICENSE)
