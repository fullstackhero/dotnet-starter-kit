[![GitHub](https://img.shields.io/github/license/fullstackhero/dotnet-webapi-boilerplate?color=2da44e&style=flat-square)](https://github.com/fullstackhero/dotnet-webapi-boilerplate/blob/master/LICENSE)
[![Discord](https://img.shields.io/discord/878181478972928011?color=%237289da&label=Discord&logo=discord&logoColor=%237289da&style=flat-square)](https://discord.gg/yQWpShsKrf)
[![Twitter](https://img.shields.io/twitter/follow/codewithmukesh?color=1DA1F2&label=Twitter&logo=Twitter&style=flat-square)](https://twitter.com/codewithmukesh)
[![Nuget version](https://img.shields.io/nuget/v/FullStackHero.WebAPI.Boilerplate?color=2da44e&label=nuget%20version&logo=nuget&style=flat-square)](https://www.nuget.org/packages/FullStackHero.WebAPI.Boilerplate/)
[![Nuget downloads](https://img.shields.io/nuget/dt/FullStackHero.WebAPI.Boilerplate?color=2da44e&label=nuget%20downloads&logo=nuget&style=flat-square)](https://www.nuget.org/packages/FullStackHero.WebAPI.Boilerplate/)

<p align="center">
    <img src="https://codewithmukesh.com/wp-content/uploads/2021/11/fullstackhero-dotnet-webapi-banner.jpg" alt="fullstackhero">
</p>

.NET WebAPI Boilerplate Template built with .NET 6.0. Incorporates the most essential Packages your projects will ever need. Follows Clean Architecture Principles.

## About

`dotnet-webapi-boilerplate` is an integral part of the `fullstackhero` project.

`fullstackhero` is a venture to develop industry-leading boilerplate templates for the dotnet stack as the backend (web API) along with modern client frameworks like Angular, MVC, and Blazor.

This repository contains the WebApi Project of `fullstackhero`.

## Release Planning

### 0.0.5 RC is available now!

- Fixed Password Reset / Forgot
- Fixed Omnisharp Warnings / Code Cleanup
- Fixed Repository Method
- Log Hangfire to Serilog - Thanks [frankyjquintero](https://github.com/frankyjquintero)
- Extras Hangfire extensions - Thanks [frankyjquintero](https://github.com/frankyjquintero)
- Hangfire implementation examples - Thanks [frankyjquintero](https://github.com/frankyjquintero)
- Separate JSON files for each Settings - Thanks [unchase](https://github.com/unchase)
- Added Events and Handlers via MediatR - Thanks [frankyjquintero](https://github.com/frankyjquintero)
- Moved to Tenancy Middleware - Thanks [frankyjquintero](https://github.com/frankyjquintero) & [fretje](https://github.com/fretje)
- Solution Cleanup - rulesets & analyzer - Thanks [fretje](https://github.com/fretje)
- Added More Functions for RepositoryAsync - Thanks [ghaithprosoft](https://github.com/ghaithprosoft)
- Entity Database Seeding Simplified
- File-Scoped namespaces and implicit usings
- Better Folder Structure / Modular

[View Complete Changelogs.](https://github.com/fullstackhero/dotnet-webapi-boilerplate/blob/main/Changelogs.md)

These are the first pre-release versions of the `fullstackhero .NET WebAPI Boilerplate` package. Newer versions will be available on a weekly basis with newer updates and patches. [Read the getting-started guide for more.](https://fullstackhero.net/dotnet-webapi-boilerplate/general/getting-started/)

The Release Version v1 is expected to be out by December 20th or earlier. Preview versions of this project are available for Initial Developer Testing. 0.0.5 RC is the most stable version of this API currently.

## Quick Start Guide

Open up your Command Prompt / Powershell and run the following command to install the solution template.

```powershell
dotnet new --install FullStackHero.WebAPI.Boilerplate
```
This would install the `fullstackhero .NET WebAPI Boilerplate` template globally on your machine. With that done, let's see how you can start generating complete .NET WebAPI Solutions seamlessly.

Simply navigate to a new directory (wherever you want to place your new solution), and open up Command Prompt at the opened directory.

Run the following command. Note that, in this demonstration, I am naming my new solution as `FSH.Starter`.

```powershell
dotnet new fsh-api -o FSH.Starter
```

For further steps and details, [Read the Getting Started Guide](https://fullstackhero.net/dotnet-webapi-boilerplate/general/getting-started/)

## Important Links & Documentations

Overview - [Read](https://fullstackhero.net/dotnet-webapi-boilerplate/general/overview/)

Getting Started - [Read](https://fullstackhero.net/dotnet-webapi-boilerplate/general/getting-started/)

Development Environment - [Learn about setting up the DEV environment](https://fullstackhero.net/dotnet-webapi-boilerplate/general/development-environment/)

Track Progress - [Release 1.0 Milestones](https://github.com/fullstackhero/dotnet-webapi-boilerplate/milestone/1)

Participate in Discussions - [QNA & General Discussions](https://github.com/fullstackhero/dotnet-webapi-boilerplate/discussions)

Join our Discord - [fullstackhero @ Discord](https://discord.gg/gdgHRt4mMw)

## Features

- [x] Built on .NET 6.0
- [x] Follows Clean Architecture Principles
- [ ] Completely Documented at [fullstackhero.net](https://fullstackhero.net)
- [x] Multi Tenancy Support
  - [x] Create Tenants with Multi Database / Shared Database Support
  - [x] Activate / Deactivate Tenants on Demand
  - [x] Upgrade Subscription of Tenants - Add More Validity Months to each tenant!
- [x] Supports MySQL, MSSQL & PostgreSQL!
- [x] Uses Entity Framework Core as DB Abstraction
- [x] Flexible Repository Pattern
- [x] Dapper Integration for Optimal Performance
- [x] Serilog Integration
- [x] Swagger Support
- [x] Mapster Integration for Quicker Mapping
- [x] API Versioning
- [x] Response Caching - Distributed Caching
- [x] Fluent Validations
- [x] Audit Logging
- [ ] Advanced User & Role Based Permission Management
- [x] Code Analysis & StyleCop Integration with Rulesets
- [x] JSON Based Localization with Caching
- [x] Hangfire Support
- [x] File Storage Service
- [ ] Test Projects
- [ ] & Much More

## Community

- Discord [@fullstackhero](https://discord.gg/gdgHRt4mMw)

## Contributors

Submit your PR and join the elite list!

<a href="https://github.com/fullstackhero/dotnet-webapi-boilerplate/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=fullstackhero/dotnet-webapi-boilerplate" />
</a>

## License

This project is licensed with the [MIT license](LICENSE).

## Support :star:

Has this Project helped you learn something New? or Helped you at work?
Here are a few ways by which you can support.

-   Leave a star! :star:
-   Recommend this awesome project to your colleagues. 🥇
-   Do consider endorsing me on LinkedIn for ASP.NET Core - [Connect via LinkedIn](https://codewithmukesh.com/linkedin) 🦸
-   Or, If you want to support this project in the long run, [consider buying me a coffee](https://www.buymeacoffee.com/codewithmukesh)! ☕

<a href="https://www.buymeacoffee.com/codewithmukesh"><img width="135" alt="black-button" src="https://user-images.githubusercontent.com/31455818/138557309-27587d91-7b82-4cab-96bb-90f4f4e600f1.png" ></a>
