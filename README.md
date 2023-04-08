[![dotnet-cicd](https://github.com/fullstackhero/dotnet-webapi-boilerplate/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/fullstackhero/dotnet-webapi-boilerplate/actions/workflows/dotnet.yml)
[![GitHub](https://img.shields.io/github/license/fullstackhero/dotnet-webapi-boilerplate?color=2da44e)](https://github.com/fullstackhero/dotnet-webapi-boilerplate/blob/master/LICENSE)
[![Discord](https://img.shields.io/discord/878181478972928011?color=%237289da&label=Discord&logo=discord&logoColor=%237289da)](https://discord.gg/yQWpShsKrf)
[![Nuget downloads](https://img.shields.io/nuget/dt/FullStackHero.WebAPI.Boilerplate?color=2da44e&label=nuget%20downloads&logo=nuget)](https://www.nuget.org/packages/FullStackHero.WebAPI.Boilerplate/)
[![Twitter](https://img.shields.io/twitter/url/https/twitter.com/iammukeshm.svg?style=social&label=Follow%20%40iammukeshm)](https://twitter.com/iammukeshm)

![fullstackhero webapi](https://raw.githubusercontent.com/fullstackhero/dotnet-webapi-boilerplate/main/media/fullstack-hero-dotnet-7-webapi-boilerplate-banner.png "fullstackhero webapi")

## What's fullstackhero's .NET Web API Boilerplate?

fullstackhero's .NET Web API Boilerplate is a starting point for your next `.NET 7 Clean Architecture Project` that incorporates the most essential packages and features your projects will ever need including out of the box Multi-Tenancy support. This project can save well over `200+ hours` of development time for your team.

> As the name suggests, this is an API / Server Boilerplate. You can find other Client Boilerplates that consume this API under `@fullstackhero` handle.
> - Find `Blazor WebAssembly Boilerplate` here - https://github.com/fullstackhero/blazor-wasm-boilerplate

## Goals

The goal of this repository is to provide a complete and feature-rich starting point for any .NET Developer / Team to kick-start their next major project using .NET 7 Web API. This also serves the purpose of learning advanced concepts and implementations such as `Multitenancy, CQRS, Onion Architecture, Clean Coding standards, Cloud Deployments with Terraform to AWS, Docker Concepts, CICD Pipelines & Workflows` and so on.

## Features

- [x] Built on .NET 7.0
- [x] Follows Clean Architecture Principles
- [x] Domain Driven Design
- [x] Cloud Ready. Can be deployed to AWS Infrastructure as ECS Containers using Terraform!
- [x] Docker-Compose File Examples
- [x] Documented at [fullstackhero.net](https://fullstackhero.net)
- [x] Multi Tenancy Support with Finbuckle
  - [x] Create Tenants with Multi Database / Shared Database Support
  - [x] Activate / Deactivate Tenants on Demand
  - [x] Upgrade Subscription of Tenants - Add More Validity Months to each tenant!
- [x] Supports MySQL, MSSQL, Oracle & PostgreSQL!

<details>
  <summary>Click to See More!</summary>

- [x] Uses Entity Framework Core as DB Abstraction
- [x] Flexible Repository Pattern
- [x] Dapper Integration for Optimal Performance
- [x] Serilog Integration with various Sinks - File, SEQ, Kibana
- [x] OpenAPI - Supports Client Service Generation
- [x] Mapster Integration for Quicker Mapping
- [x] API Versioning
- [x] Response Caching - Distributed Caching + REDIS
- [x] Fluent Validations
- [x] Audit Logging
- [x] Advanced User & Role Based Permission Management
- [x] Code Analysis & StyleCop Integration with Rulesets
- [x] JSON Based Localization with Caching
- [x] Hangfire Support - Secured Dashboard
- [x] File Storage Service
- [x] Test Projects
- [x] JWT & Azure AD Authentication
- [x] MediatR - CQRS
- [x] SignalR Notifications
- [x] & Much More
</details>

## Documentation

Read Documentation related to this Boilerplate here - https://fullstackhero.net/dotnet-webapi-boilerplate/
> Feel free to contribute to the Documentation Repository - https://github.com/fullstackhero/docs

## Getting Started

To get started with this Boilerplate, here are the available options.

- Install using the `FSH CLI` tool. Use this for release versions of the Boilerplate only.
- Fork the Repository. Use this if you want to always keep your version of the Boilerplate up-to date with the latest changes.

> Make sure that your DEV enviroment is setup, [Read the Development Environment Guide](https://fullstackhero.net/dotnet-webapi-boilerplate/general/development-environment/)

### FSH CLI Tool

#### Prerequisites

Before creating your first fullstackhero solution, you should ensure that your local machine has:

- **.NET 7** You can find the download [here](https://dotnet.microsoft.com/en-us/download/dotnet/7.0).
- **NodeJS (16+)** You can find the download [here](https://nodejs.org/en/download).

#### Installation

After you have installed .NET, you will need to install the `fsh` console tool.

```bash
dotnet tool install --global FSH.CLI
fsh install
```

This isntall the FSH CLI tools and the associated Templates. You are now ready to create your first FSH project!

#### FSH .NET WebAPI Boilerplate
Here's how you would create a Solution using the FSH .NET WebAPI Boilerplate.

Simply navigate to a new directory (wherever you want to place your new solution), and open up Command Prompt at the opened directory.

Run the following command. Note that, in this demonstration, I am naming my new solution as FSH.Starter.

```bash
fsh api new FSH.Starter
```

OR

```bash
fsh api n FSH.Starter
```

This will create a new .NET 7 WEBAPI solution for you using the FSH Templates.
For further steps and details, [Read the Getting Started Guide](https://fullstackhero.net/dotnet-webapi-boilerplate/general/getting-started/)

#### Update
To update the tool & templates, run the following commands
```bash
dotnet tool update FSH.CLI --global
fsh update
```
### Forking the Repository

You would probably need to take this approach if you want to keep your source code upto date with the latest changes. To get started based on this repository, you need to get a copy locally. You have three options: fork, clone, or download.

- Make a fork of this repository in your Github account.
- Create your new `dotnet-webapi-boilerplate` personal project by cloning the forked repository on your personal github.
- Setup an upstream remote on your personal project pointing to your forked repository using command `git remote add upstream https://github.com/{githubuseraccount}/dotnet-webapi-boilerplate` and `git remote set-url --push upstream DISABLE`

For step by step instructions, [follow this](https://discord.com/channels/878181478972928011/892573122186838046/933513103688224838) and [this](https://gist.github.com/0xjac/85097472043b697ab57ba1b1c7530274).


## Important Links & Documentations

Overview - [Read](https://fullstackhero.net/dotnet-webapi-boilerplate/general/overview/)

Getting Started - [Read](https://fullstackhero.net/dotnet-webapi-boilerplate/general/getting-started/)

Development Environment - [Learn about setting up the DEV environment](https://fullstackhero.net/dotnet-webapi-boilerplate/general/development-environment/)

Participate in Discussions - [QNA & General Discussions](https://github.com/fullstackhero/dotnet-webapi-boilerplate/discussions)

Join our Discord - [fullstackhero @ Discord](https://discord.gg/gdgHRt4mMw)

## Changelogs

[View Complete Changelogs.](https://github.com/fullstackhero/dotnet-webapi-boilerplate/blob/main/Changelogs.md)

## Community

- Discord [@fullstackhero](https://discord.gg/gdgHRt4mMw)
- Facebook Page [@codewithmukesh](https://facebook.com/codewithmukesh)
- Youtube Channel [@codewithmukesh](https://youtube.com/c/codewithmukesh)

## Contributors

Submit your PR and join the elite list!

<a href="https://github.com/fullstackhero/dotnet-webapi-boilerplate/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=fullstackhero/dotnet-webapi-boilerplate" />
</a>

## License

This project is licensed with the [MIT license](LICENSE).

## Support ⭐

Has this Project helped you learn something New? or Helped you at work?
Here are a few ways by which you can support.

-   Leave a star! ⭐
-   Recommend this awesome project to your colleagues. 🥇
-   Do consider endorsing me on LinkedIn for ASP.NET Core - [Connect via LinkedIn](https://codewithmukesh.com/linkedin) 🦸
-   Sponsor the project - [opencollective/fullstackhero](https://opencollective.com/fullstackhero) ❤️
-   Or, [consider buying me a coffee](https://www.buymeacoffee.com/codewithmukesh)! ☕

[![buy-me-a-coffee](https://raw.githubusercontent.com/fullstackhero/dotnet-webapi-boilerplate/main/media/buy-me-a-coffee.png "buy-me-a-coffee")](https://www.buymeacoffee.com/codewithmukesh)