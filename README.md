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



## YouTube Video - .NET Web API Boilerplate | FullStackHero - Getting Started

`Watch the Getting started video here` : https://www.youtube.com/watch?v=a1mWRLQf9hY

[![.NET Web API Boilerplate | FullStackHero - Getting Started](https://codewithmukesh.com/wp-content/uploads/2023/04/fullstackhero-youtube.png)](https://www.youtube.com/watch?v=a1mWRLQf9hY)


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


## Quick Start Guide

So, for a better developer experience, I have added Makefile into the solution. Now that our solution is generated, let's navigate to the root folder of the solution and open up a command terminal.

To build the solution,
```
make build
```

By default, the solution is configured to work with postgresql database (mainly because of hte OS licensing). So, you will have to make sure that postgresql database instance is up and running on your machine. You can modify the connection string to include your username and password. Connections strings can be found at `src/Host/Configurations/database.json` and `src/Host/Configurations/hangfire.json`. Once that's done, let's start up the API server.

```
make start
```

That's it, the application would connect to the defined postgresql database and start creating tables, and seed required data.

For testing this API, we have 3 options.
1. Swagger @ `localhost:5001/swagger`
2. Postman collections are available `./postman`
3. ThunderClient for VSCode. This is my personal favorite. You will have to install the Thunderclient extension for VSCode.

The default credentials to this API is:


```json
{
    "email":"admin@root.com",
    "password":"123Pa$$word!"
}
```

Open up Postman, Thunderclient or Swagger.

identity -> get-token

This is a POST Request. Here the body of the request will be the JSON (credentials) I specified earlier. And also, remember to pass the tenant id in the header of the request. The default tenant id is `root`.

Here is a sample CURL command for getting the tokens.

```curl
curl -X POST \
  'https://localhost:5001/api/tokens' \
  --header 'Accept: */*' \
  --header 'tenant: root' \
  --header 'Accept-Language: en-US' \
  --header 'Content-Type: application/json' \
  --data-raw '{
  "email": "admin@root.com",
  "password": "123Pa$$word!"
}'
```

And here is the response.

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjM0YTY4ZjQyLWE0ZDgtNDNlMy1hNzE3LTI1OTczZjZmZTJjNyIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL2VtYWlsYWRkcmVzcyI6ImFkbWluQHJvb3QuY29tIiwiZnVsbE5hbWUiOiJyb290IEFkbWluIiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbmFtZSI6InJvb3QiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9zdXJuYW1lIjoiQWRtaW4iLCJpcEFkZHJlc3MiOiIxMjcuMC4wLjEiLCJ0ZW5hbnQiOiJyb290IiwiaW1hZ2VfdXJsIjoiIiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbW9iaWxlcGhvbmUiOiIiLCJleHAiOjE2ODA5NDE3MzN9.VYNaNvk2T4YDvQ3wriXgk2W_Vy9zyEEhjveNauNAeJY",
  "refreshToken": "pyxO30zJK8KelpEXF0vPfbSbjntdlbbnxrZAlUFXfyE=",
  "refreshTokenExpiryTime": "2023-04-15T07:15:33.5187598Z"
}
```

You will need to pass the `token` in the request headers to authenticate calls to the fullstackhero API!

For further steps and details, [Read the Getting Started Guide](https://fullstackhero.net/dotnet-webapi-boilerplate/general/getting-started/)

## Containerization

The API project, being .NET 7, it is configured to have built-in support for containerization. That means, you really don't need a Dockerfile to containerize the webapi.

To build a docker image, all you have to do is, ensure that docker-desktop or docker instance is running. And run the following command at the root of the solution.

```
make publish
```

You can also push the docker image directly to dockerhub or any supported registry by using the following command.

```
make publish-to-hub
```
You will have to update your docker registry / repo url in the Makefile though!.

## Docker Compose

This project also comes with examples of docker compose files, where you can spin up the webapi and database isntance in your local containers with the following commands.

```powershell
make dcu #docker compose up - Boots up the webapi & postgresql container
make dcd #docker compose down - Shuts down the webapi & postgresql containers
```

There are also examples for mysql & mssql variations of the fsh webapi. You can find the other docker-compose files under the ./docker-compose folder. Read more about [fullstackhero's docker-compose instructions & files here](./docker-compose/README.md)

## Cloud Deployment with Terraform + AWS ECS

This is something you wont get to see very often with boilerplates. But, we do support cloud deployment to AWS using terraform. The terraform files are available at the `./terraform` folder.

### Prerequisites
- Install Terraform
- Install & Configure AWS CLI profiles to allow terraform to provision resources for you. I have made a video about [AWS Credentials Management](https://www.youtube.com/watch?v=oY0-1mj4oCo&ab_channel=MukeshMurugan).

In brief, the terraform folder has 2 sub-folders.
- backend
- environments/staging

The Backend folder is internally used by Terraform for state management and locking. There is a one-time setup you have to do against this folder. Navigate to the backend folder and run the command.

```
terraform init
terraform apply -auto-approve
```

This would create the required S3 Buckets and DDB table for you.

Next is the `environments/staging` folder. Here too, run the following command.

```
terraform init
```

Once done, you can go the terraform.tfvars file to change the variables like,
- project tags
- docker image name
- ecs cluster name and so on.

After that, simply back to the root of the solution and run the following command.

```
make ta
```

This will evaluate your terraform files and create a provision plan for you. Once you are ok, type in `yes` and the tool will start to deploy your .NET WebAPI project as containers along with a RDS PostgreSQL intance. You will be receiving the hosted api url once the provisioning is completed!

To destroy the deployed resources, run the following
```
make td
```

## Important Links & Documentations

Overview - [Read](https://fullstackhero.net/dotnet-webapi-boilerplate/general/overview/)

Getting Started - [Read](https://fullstackhero.net/dotnet-webapi-boilerplate/general/getting-started/)

Development Environment - [Learn about setting up the DEV environment](https://fullstackhero.net/dotnet-webapi-boilerplate/general/development-environment/)

Participate in Discussions - [QNA & General Discussions](https://github.com/fullstackhero/dotnet-webapi-boilerplate/discussions)

Join our Discord - [fullstackhero @ Discord](https://discord.gg/gdgHRt4mMw)

## Changelogs

[View Complete Changelogs.](https://github.com/fullstackhero/dotnet-webapi-boilerplate/blob/main/CHANGELOGS.md)

## Community

- Discord [@fullstackhero](https://discord.gg/gdgHRt4mMw)
- Facebook Page [@codewithmukesh](https://facebook.com/codewithmukesh)
- Youtube Channel [@codewithmukesh](https://youtube.com/c/codewithmukesh)

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

## Code Contributors

This project exists thanks to all the people who contribute. [Submit your PR and join the elite list!](CONTRIBUTING.md)

[![fsh dotnet webapi contributors](https://contrib.rocks/image?repo=fullstackhero/dotnet-webapi-boilerplate "fsh dotnet webapi contributors")](https://github.com/fullstackhero/dotnet-webapi-boilerplate/graphs/contributors)

## Financial Contributors

Become a financial contributor and help me sustain the project. [Support the Project!](https://opencollective.com/fullstackhero/contribute)

<a href="https://opencollective.com/fullstackhero"><img src="https://opencollective.com/fullstackhero/individuals.svg?width=890"></a>
