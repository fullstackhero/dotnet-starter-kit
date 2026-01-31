using System.Diagnostics.CodeAnalysis;
using FSH.CLI.Models;

namespace FSH.CLI.Scaffolding;

/// <summary>
/// Renders templates with variable substitution
/// </summary>
[SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Lowercase is required for Docker, Terraform, and GitHub Actions naming conventions")]
internal sealed class TemplateRenderer : ITemplateRenderer
{
    private readonly ITemplateLoader _templateLoader;
    private readonly ITemplateParser _templateParser;
    private readonly ITemplateCache _templateCache;

    public TemplateRenderer(ITemplateLoader templateLoader, ITemplateParser templateParser, ITemplateCache templateCache)
    {
        _templateLoader = templateLoader ?? throw new ArgumentNullException(nameof(templateLoader));
        _templateParser = templateParser ?? throw new ArgumentNullException(nameof(templateParser));
        _templateCache = templateCache ?? throw new ArgumentNullException(nameof(templateCache));
    }

    #region Solution and Project Templates

    public string RenderSolution(ProjectOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var projects = new List<string>
        {
            $"""    <Project Path="{options.Name}.Api/{options.Name}.Api.csproj" />""",
            $"""    <Project Path="{options.Name}.Migrations/{options.Name}.Migrations.csproj" />"""
        };

        if (options.Type == ProjectType.ApiBlazor)
        {
            projects.Add($"""    <Project Path="{options.Name}.Blazor/{options.Name}.Blazor.csproj" />""");
        }

        if (options.IncludeAspire)
        {
            projects.Add($"""    <Project Path="{options.Name}.AppHost/{options.Name}.AppHost.csproj" />""");
        }

        if (options.IncludeSampleModule)
        {
            projects.Add($"""    <Project Path="Modules/{options.Name}.Catalog/{options.Name}.Catalog.csproj" />""");
            projects.Add($"""    <Project Path="Modules/{options.Name}.Catalog.Contracts/{options.Name}.Catalog.Contracts.csproj" />""");
        }

        return $$"""
            <Solution>
              <Folder Name="/src/">
            {{string.Join(Environment.NewLine, projects)}}
              </Folder>
              <Folder Name="/Solution Items/">
                <File Path="../.gitignore" />
                <File Path="Directory.Build.props" />
                <File Path="Directory.Packages.props" />
              </Folder>
            </Solution>
            """;
    }

    public string RenderApiCsproj(ProjectOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var serverless = options.Architecture == ArchitectureStyle.Serverless;

        var sampleModuleRef = options.IncludeSampleModule
            ? $"""

              <ItemGroup>
                <!-- Sample Module -->
                <ProjectReference Include="..\Modules\{options.Name}.Catalog\{options.Name}.Catalog.csproj" />
              </ItemGroup>
            """
            : string.Empty;

        return $$"""
            <Project Sdk="Microsoft.NET.Sdk.Web">

              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
            {{(serverless ? "    <OutputType>Library</OutputType>" : "")}}
              </PropertyGroup>

              <ItemGroup>
                <!-- FullStackHero Framework packages -->
                <PackageReference Include="FullStackHero.Framework.Core" />
                <PackageReference Include="FullStackHero.Framework.Persistence" />
                <PackageReference Include="FullStackHero.Framework.Caching" />
                <PackageReference Include="FullStackHero.Framework.Web" />
                <!-- FullStackHero Modules -->
                <PackageReference Include="FullStackHero.Modules.Identity" />
                <PackageReference Include="FullStackHero.Modules.Identity.Contracts" />
                <PackageReference Include="FullStackHero.Modules.Multitenancy" />
                <PackageReference Include="FullStackHero.Modules.Multitenancy.Contracts" />
                <PackageReference Include="FullStackHero.Modules.Auditing" />
                <PackageReference Include="FullStackHero.Modules.Auditing.Contracts" />
                <!-- Mediator -->
                <PackageReference Include="Mediator.Abstractions" />
                <PackageReference Include="Mediator.SourceGenerator">
                  <PrivateAssets>all</PrivateAssets>
                  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
                </PackageReference>
              </ItemGroup>
            {{(serverless ? """

              <ItemGroup>
                <!-- AWS Lambda -->
                <PackageReference Include="Amazon.Lambda.AspNetCoreServer.Hosting" />
              </ItemGroup>
            """ : "")}}{{sampleModuleRef}}
            </Project>
            """;
    }

    public string RenderApiProgram(ProjectOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var serverless = options.Architecture == ArchitectureStyle.Serverless;

        if (serverless)
        {
            var serverlessModuleUsing = options.IncludeSampleModule
                ? $"using {options.Name}.Catalog;\n"
                : string.Empty;

            var serverlessModuleAssembly = options.IncludeSampleModule
                ? $",\n    typeof(CatalogModule).Assembly"
                : string.Empty;

            return $$"""
                {{serverlessModuleUsing}}using FSH.Framework.Web;
                using FSH.Framework.Web.Modules;
                using System.Reflection;

                var builder = WebApplication.CreateBuilder(args);

                // Add AWS Lambda hosting
                builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi);

                // Add FSH Platform
                builder.AddHeroPlatform(platform =>
                {
                    platform.EnableOpenApi = true;
                    platform.EnableCaching = true;
                });

                // Add modules
                var moduleAssemblies = new Assembly[]
                {
                    typeof(Program).Assembly{{serverlessModuleAssembly}}
                };
                builder.AddModules(moduleAssemblies);

                var app = builder.Build();

                // Use FSH Platform
                app.UseHeroPlatform(platform =>
                {
                    platform.MapModules = true;
                });

                await app.RunAsync();
                """;
        }

        var sampleModuleUsing = options.IncludeSampleModule
            ? $"using {options.Name}.Catalog;\n"
            : string.Empty;

        var sampleModuleAssembly = options.IncludeSampleModule
            ? ",\n    typeof(CatalogModule).Assembly"
            : string.Empty;

        return $$"""
            {{sampleModuleUsing}}using FSH.Framework.Web;
            using FSH.Framework.Web.Modules;
            using FSH.Modules.Auditing;
            using FSH.Modules.Identity;
            using FSH.Modules.Identity.Contracts.v1.Tokens.TokenGeneration;
            using FSH.Modules.Identity.Features.v1.Tokens.TokenGeneration;
            using FSH.Modules.Multitenancy;
            using FSH.Modules.Multitenancy.Contracts.v1.GetTenantStatus;
            using FSH.Modules.Multitenancy.Features.v1.GetTenantStatus;
            using System.Reflection;

            var builder = WebApplication.CreateBuilder(args);

            // Configure Mediator with required assemblies
            builder.Services.AddMediator(o =>
            {
                o.ServiceLifetime = ServiceLifetime.Scoped;
                o.Assemblies = [
                    typeof(GenerateTokenCommand),
                    typeof(GenerateTokenCommandHandler),
                    typeof(GetTenantStatusQuery),
                    typeof(GetTenantStatusQueryHandler),
                    typeof(FSH.Modules.Auditing.Contracts.AuditEnvelope),
                    typeof(FSH.Modules.Auditing.Persistence.AuditDbContext)];
            });

            // FSH Module assemblies
            var moduleAssemblies = new Assembly[]
            {
                typeof(IdentityModule).Assembly,
                typeof(MultitenancyModule).Assembly,
                typeof(AuditingModule).Assembly{{sampleModuleAssembly}}
            };

            // Add FSH Platform
            builder.AddHeroPlatform(platform =>
            {
                platform.EnableOpenApi = true;
                platform.EnableCaching = true;
                platform.EnableJobs = true;
                platform.EnableMailing = true;
            });

            // Add modules
            builder.AddModules(moduleAssemblies);

            var app = builder.Build();

            // Apply tenant database migrations
            app.UseHeroMultiTenantDatabases();

            // Use FSH Platform
            app.UseHeroPlatform(platform =>
            {
                platform.MapModules = true;
            });

            await app.RunAsync();
            """;
    }

    public string RenderMigrationsCsproj(ProjectOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var dbPackage = options.Database switch
        {
            DatabaseProvider.PostgreSQL => "<PackageReference Include=\"Npgsql.EntityFrameworkCore.PostgreSQL\" />",
            DatabaseProvider.SqlServer => "<PackageReference Include=\"Microsoft.EntityFrameworkCore.SqlServer\" />",
            DatabaseProvider.SQLite => "<PackageReference Include=\"Microsoft.EntityFrameworkCore.Sqlite\" />",
            _ => string.Empty
        };

        return $$"""
            <Project Sdk="Microsoft.NET.Sdk">

              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>

              <ItemGroup>
                <PackageReference Include="Microsoft.EntityFrameworkCore.Design" />
                {{dbPackage}}
              </ItemGroup>

              <ItemGroup>
                <ProjectReference Include="..\{{options.Name}}.Api\{{options.Name}}.Api.csproj" />
              </ItemGroup>

            </Project>
            """;
    }

    public string RenderBlazorCsproj() => _templateLoader.GetStaticTemplate("BlazorCsproj");

    public string RenderBlazorProgram(ProjectOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return $$"""
            using Microsoft.AspNetCore.Components.Web;
            using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
            using MudBlazor.Services;
            using {{options.Name}}.Blazor;

            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            builder.Services.AddMudServices();

            await builder.Build().RunAsync();
            """;
    }

    public string RenderAppHostCsproj(ProjectOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var dbPackage = options.Database switch
        {
            DatabaseProvider.PostgreSQL => "<PackageReference Include=\"Aspire.Hosting.PostgreSQL\" />",
            DatabaseProvider.SqlServer => "<PackageReference Include=\"Aspire.Hosting.SqlServer\" />",
            _ => string.Empty // SQLite doesn't need a hosting package
        };

        return $$"""
            <Project Sdk="Aspire.AppHost.Sdk/13.1.0">

              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <IsPackable>false</IsPackable>
              </PropertyGroup>

              <ItemGroup>
                {{dbPackage}}
                <PackageReference Include="Aspire.Hosting.Redis" />
              </ItemGroup>

              <ItemGroup>
                <ProjectReference Include="..\{{options.Name}}.Api\{{options.Name}}.Api.csproj" />
            {{(options.Type == ProjectType.ApiBlazor ? $"    <ProjectReference Include=\"..\\{options.Name}.Blazor\\{options.Name}.Blazor.csproj\" />" : "")}}
              </ItemGroup>

            </Project>
            """;
    }

    public string RenderAppHostProgram(ProjectOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var projectNameLower = _templateParser.NormalizeProjectName(options.Name, NameContext.LowerCase);
        var projectNameSafe = _templateParser.NormalizeProjectName(options.Name, NameContext.SafeIdentifier);

        var (dbSetup, dbProvider, dbRef, dbWait, migrationsAssembly) = options.Database switch
        {
            DatabaseProvider.PostgreSQL => (
                $"""
                // Postgres container + database
                var postgres = builder.AddPostgres("postgres").WithDataVolume("{projectNameLower}-postgres-data").AddDatabase("{projectNameLower}");
                """,
                "POSTGRESQL",
                ".WithReference(postgres)",
                ".WaitFor(postgres)",
                $"{options.Name}.Migrations"),
            DatabaseProvider.SqlServer => (
                $"""
                // SQL Server container + database
                var sqlserver = builder.AddSqlServer("sqlserver").WithDataVolume("{projectNameLower}-sqlserver-data").AddDatabase("{projectNameLower}");
                """,
                "MSSQL",
                ".WithReference(sqlserver)",
                ".WaitFor(sqlserver)",
                $"{options.Name}.Migrations"),
            DatabaseProvider.SQLite => (
                "// SQLite runs embedded - no container needed",
                "SQLITE",
                string.Empty,
                string.Empty,
                $"{options.Name}.Migrations"),
            _ => ("// Database configured externally", "POSTGRESQL", string.Empty, string.Empty, $"{options.Name}.Migrations")
        };

        var redisSetup = $"""
            var redis = builder.AddRedis("redis").WithDataVolume("{projectNameLower}-redis-data");
            """;

        // Build database environment variables
        var dbResourceName = options.Database == DatabaseProvider.PostgreSQL ? "postgres" : "sqlserver";
        var dbEnvVars = options.Database != DatabaseProvider.SQLite
            ? $$"""
                .WithEnvironment("DatabaseOptions__Provider", "{{dbProvider}}")
                .WithEnvironment("DatabaseOptions__ConnectionString", {{dbResourceName}}.Resource.ConnectionStringExpression)
                .WithEnvironment("DatabaseOptions__MigrationsAssembly", "{{migrationsAssembly}}")
                {{dbWait}}
                """
            : """
                .WithEnvironment("DatabaseOptions__Provider", "SQLITE")
                """;

        // When Blazor is included, api variable is referenced; otherwise suppress unused warning
        var (apiDeclaration, blazorProject) = options.Type == ProjectType.ApiBlazor
            ? ($"var api = builder.AddProject<Projects.{projectNameSafe}_Api>(\"{projectNameLower}-api\")",
               $"""

                builder.AddProject<Projects.{projectNameSafe}_Blazor>("{projectNameLower}-blazor");
                """)
            : ($"builder.AddProject<Projects.{projectNameSafe}_Api>(\"{projectNameLower}-api\")", string.Empty);

        return $$"""
            var builder = DistributedApplication.CreateBuilder(args);

            {{dbSetup}}

            {{redisSetup}}

            {{apiDeclaration}}
                {{dbRef}}
                .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
                {{dbEnvVars}}
                .WithReference(redis)
                .WithEnvironment("CachingOptions__Redis", redis.Resource.ConnectionStringExpression)
                .WaitFor(redis);
            {{blazorProject}}

            await builder.Build().RunAsync();
            """;
    }

    #endregion

    #region Configuration Templates

    public string RenderAppSettings(ProjectOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var connectionString = options.Database switch
        {
            DatabaseProvider.PostgreSQL => $"Server=localhost;Database={_templateParser.NormalizeProjectName(options.Name, NameContext.LowerCase)};User Id=postgres;Password=password",
            DatabaseProvider.SqlServer => $"Server=localhost;Database={options.Name};Trusted_Connection=True;TrustServerCertificate=True",
            DatabaseProvider.SQLite => $"Data Source={options.Name}.db",
            _ => string.Empty
        };

        var dbProvider = options.Database switch
        {
            DatabaseProvider.PostgreSQL => "POSTGRESQL",
            DatabaseProvider.SqlServer => "MSSQL",
            DatabaseProvider.SQLite => "SQLITE",
            _ => "POSTGRESQL"
        };

        var migrationsAssembly = $"{options.Name}.Migrations";
        var projectNameLower = _templateParser.NormalizeProjectName(options.Name, NameContext.LowerCase);

        return $$"""
            {
              "OpenTelemetryOptions": {
                "Enabled": true,
                "Tracing": {
                  "Enabled": true
                },
                "Metrics": {
                  "Enabled": true,
                  "MeterNames": []
                },
                "Exporter": {
                  "Otlp": {
                    "Enabled": true,
                    "Endpoint": "http://localhost:4317",
                    "Protocol": "grpc"
                  }
                },
                "Jobs": { "Enabled": true },
                "Mediator": { "Enabled": true },
                "Http": {
                  "Histograms": {
                    "Enabled": true
                  }
                },
                "Data": {
                  "FilterEfStatements": true,
                  "FilterRedisCommands": true
                }
              },
              "Serilog": {
                "Using": [
                  "Serilog.Sinks.Console",
                  "Serilog.Sinks.OpenTelemetry"
                ],
                "Enrich": [ "FromLogContext", "WithMachineName", "WithThreadId", "WithCorrelationId", "WithProcessId", "WithProcessName" ],
                "MinimumLevel": {
                  "Default": "Debug"
                },
                "WriteTo": [
                  {
                    "Name": "Console",
                    "Args": {
                      "restrictedToMinimumLevel": "Information"
                    }
                  },
                  {
                    "Name": "OpenTelemetry",
                    "Args": {
                      "endpoint": "http://localhost:4317",
                      "protocol": "grpc",
                      "resourceAttributes": {
                        "service.name": "{{options.Name}}.Api"
                      }
                    }
                  }
                ]
              },
              "Logging": {
                "LogLevel": {
                  "Default": "Information",
                  "Microsoft.AspNetCore": "Warning",
                  "Hangfire": "Warning",
                  "Microsoft.EntityFrameworkCore": "Warning"
                }
              },
              "DatabaseOptions": {
                "Provider": "{{dbProvider}}",
                "ConnectionString": "{{connectionString}}",
                "MigrationsAssembly": "{{migrationsAssembly}}"
              },
              "OriginOptions": {
                "OriginUrl": "https://localhost:7030"
              },
              "CachingOptions": {
                "Redis": ""
              },
              "HangfireOptions": {
                "Username": "admin",
                "Password": "Secure1234!Me",
                "Route": "/jobs"
              },
              "AllowedHosts": "*",
              "OpenApiOptions": {
                "Enabled": true,
                "Title": "{{options.Name}} API",
                "Version": "v1",
                "Description": "{{options.Name}} API built with FullStackHero .NET Starter Kit.",
                "Contact": {
                  "Name": "Your Name",
                  "Url": "https://yourwebsite.com",
                  "Email": "your@email.com"
                },
                "License": {
                  "Name": "MIT License",
                  "Url": "https://opensource.org/licenses/MIT"
                }
              },
              "CorsOptions": {
                "AllowAll": false,
                "AllowedOrigins": [
                  "https://localhost:4200",
                  "https://localhost:7140"
                ],
                "AllowedHeaders": [ "content-type", "authorization" ],
                "AllowedMethods": [ "GET", "POST", "PUT", "DELETE" ]
              },
              "JwtOptions": {
                "Issuer": "{{projectNameLower}}.local",
                "Audience": "{{projectNameLower}}.clients",
                "SigningKey": "replace-with-256-bit-secret-min-32-chars",
                "AccessTokenMinutes": 2,
                "RefreshTokenDays": 7
              },
              "SecurityHeadersOptions": {
                "Enabled": true,
                "ExcludedPaths": [ "/scalar", "/openapi" ],
                "AllowInlineStyles": true,
                "ScriptSources": [],
                "StyleSources": []
              },
              "MailOptions": {
                "From": "noreply@{{projectNameLower}}.com",
                "Host": "smtp.ethereal.email",
                "Port": 587,
                "UserName": "your-smtp-user",
                "Password": "your-smtp-password",
                "DisplayName": "{{options.Name}}"
              },
              "RateLimitingOptions": {
                "Enabled": false,
                "Global": {
                  "PermitLimit": 100,
                  "WindowSeconds": 60,
                  "QueueLimit": 0
                },
                "Auth": {
                  "PermitLimit": 10,
                  "WindowSeconds": 60,
                  "QueueLimit": 0
                }
              },
              "MultitenancyOptions": {
                "RunTenantMigrationsOnStartup": true
              },
              "Storage": {
                "Provider": "local"
              }
            }
            """;
    }

    public string RenderAppSettingsDevelopment() => _templateLoader.GetStaticTemplate("AppSettingsDevelopment");

    public string RenderApiLaunchSettings(ProjectOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return $$"""
            {
              "$schema": "https://json.schemastore.org/launchsettings.json",
              "profiles": {
                "http": {
                  "commandName": "Project",
                  "dotnetRunMessages": true,
                  "launchBrowser": true,
                  "launchUrl": "openapi",
                  "applicationUrl": "http://localhost:5000",
                  "environmentVariables": {
                    "ASPNETCORE_ENVIRONMENT": "Development"
                  }
                },
                "https": {
                  "commandName": "Project",
                  "dotnetRunMessages": true,
                  "launchBrowser": true,
                  "launchUrl": "openapi",
                  "applicationUrl": "https://localhost:7000;http://localhost:5000",
                  "environmentVariables": {
                    "ASPNETCORE_ENVIRONMENT": "Development"
                  }
                }
              }
            }
            """;
    }

    public string RenderAppHostLaunchSettings(ProjectOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return $$"""
            {
              "$schema": "https://json.schemastore.org/launchsettings.json",
              "profiles": {
                "https": {
                  "commandName": "Project",
                  "dotnetRunMessages": true,
                  "launchBrowser": true,
                  "applicationUrl": "https://localhost:17000;http://localhost:15000",
                  "environmentVariables": {
                    "ASPNETCORE_ENVIRONMENT": "Development",
                    "DOTNET_ENVIRONMENT": "Development",
                    "DOTNET_DASHBOARD_OTLP_ENDPOINT_URL": "https://localhost:21000",
                    "DOTNET_RESOURCE_SERVICE_ENDPOINT_URL": "https://localhost:22000"
                  }
                }
              }
            }
            """;
    }

    #endregion

    #region Blazor Templates

    public string RenderBlazorApp() => _templateLoader.GetStaticTemplate("BlazorApp");

    public string RenderBlazorImports(ProjectOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return $$"""
            @using System.Net.Http
            @using System.Net.Http.Json
            @using Microsoft.AspNetCore.Components.Forms
            @using Microsoft.AspNetCore.Components.Routing
            @using Microsoft.AspNetCore.Components.Web
            @using Microsoft.AspNetCore.Components.Web.Virtualization
            @using Microsoft.AspNetCore.Components.WebAssembly.Http
            @using Microsoft.JSInterop
            @using MudBlazor
            @using {{options.Name}}.Blazor
            """;
    }

    public string RenderBlazorIndexPage(ProjectOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return $$"""
            @page "/"

            <PageTitle>{{options.Name}}</PageTitle>

            <MudContainer MaxWidth="MaxWidth.Medium" Class="mt-8">
                <MudText Typo="Typo.h3" Class="mb-4">Welcome to {{options.Name}}</MudText>
                <MudText Typo="Typo.body1">
                    Built with FullStackHero .NET Starter Kit
                </MudText>
            </MudContainer>
            """;
    }

    public string RenderBlazorMainLayout(ProjectOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return $$"""
            @inherits LayoutComponentBase

            <MudThemeProvider />
            <MudPopoverProvider />
            <MudDialogProvider />
            <MudSnackbarProvider />

            <MudLayout>
                <MudAppBar Elevation="1">
                    <MudIconButton Icon="@Icons.Material.Filled.Menu" Color="Color.Inherit" Edge="Edge.Start" OnClick="@ToggleDrawer" />
                    <MudText Typo="Typo.h5" Class="ml-3">{{options.Name}}</MudText>
                    <MudSpacer />
                    <MudIconButton Icon="@Icons.Material.Filled.Brightness4" Color="Color.Inherit" />
                </MudAppBar>
                <MudDrawer @bind-Open="_drawerOpen" ClipMode="DrawerClipMode.Always" Elevation="2">
                    <MudNavMenu>
                        <MudNavLink Href="/" Match="NavLinkMatch.All" Icon="@Icons.Material.Filled.Home">Home</MudNavLink>
                    </MudNavMenu>
                </MudDrawer>
                <MudMainContent Class="mt-16 pa-4">
                    @Body
                </MudMainContent>
            </MudLayout>

            @code {
                private bool _drawerOpen = true;

                private void ToggleDrawer()
                {
                    _drawerOpen = !_drawerOpen;
                }
            }
            """;
    }

    #endregion

    #region Infrastructure Templates

    public string RenderDockerfile(ProjectOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return $$"""
            FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
            WORKDIR /app
            EXPOSE 8080
            EXPOSE 8081

            FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
            ARG BUILD_CONFIGURATION=Release
            WORKDIR /src
            COPY ["src/{{options.Name}}.Api/{{options.Name}}.Api.csproj", "{{options.Name}}.Api/"]
            RUN dotnet restore "{{options.Name}}.Api/{{options.Name}}.Api.csproj"
            COPY src/ .
            WORKDIR "/src/{{options.Name}}.Api"
            RUN dotnet build "{{options.Name}}.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

            FROM build AS publish
            ARG BUILD_CONFIGURATION=Release
            RUN dotnet publish "{{options.Name}}.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

            FROM base AS final
            WORKDIR /app
            COPY --from=publish /app/publish .
            ENTRYPOINT ["dotnet", "{{options.Name}}.Api.dll"]
            """;
    }

    public string RenderDockerCompose(ProjectOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var projectNameLower = _templateParser.NormalizeProjectName(options.Name, NameContext.DockerImage);

        var dbService = options.Database switch
        {
            DatabaseProvider.PostgreSQL => $"""
              postgres:
                image: postgres:16-alpine
                container_name: postgres
                environment:
                  POSTGRES_USER: postgres
                  POSTGRES_PASSWORD: postgres
                  POSTGRES_DB: {projectNameLower}
                ports:
                  - "5432:5432"
                volumes:
                  - postgres_data:/var/lib/postgresql/data
                healthcheck:
                  test: ["CMD-SHELL", "pg_isready -U postgres"]
                  interval: 10s
                  timeout: 5s
                  retries: 5
            """,
            DatabaseProvider.SqlServer => """
              sqlserver:
                image: mcr.microsoft.com/mssql/server:2022-latest
                container_name: sqlserver
                environment:
                  ACCEPT_EULA: "Y"
                  SA_PASSWORD: "Your_password123"
                ports:
                  - "1433:1433"
                volumes:
                  - sqlserver_data:/var/opt/mssql
            """,
            _ => string.Empty
        };

        var volumes = options.Database switch
        {
            DatabaseProvider.PostgreSQL => """
            volumes:
              postgres_data:
              redis_data:
            """,
            DatabaseProvider.SqlServer => """
            volumes:
              sqlserver_data:
              redis_data:
            """,
            _ => """
            volumes:
              redis_data:
            """
        };

        return $$"""
            version: '3.8'

            services:
            {{dbService}}

              redis:
                image: redis:7-alpine
                container_name: redis
                ports:
                  - "6379:6379"
                volumes:
                  - redis_data:/data
                healthcheck:
                  test: ["CMD", "redis-cli", "ping"]
                  interval: 10s
                  timeout: 5s
                  retries: 5

            {{volumes}}
            """;
    }

    public string RenderDockerComposeOverride() => _templateLoader.GetStaticTemplate("DockerComposeOverride");

    public string RenderTerraformMain(ProjectOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var serverless = options.Architecture == ArchitectureStyle.Serverless;
        var projectNameLower = _templateParser.NormalizeProjectName(options.Name, NameContext.DockerImage);

        if (serverless)
        {
            return $$"""
                terraform {
                  required_version = ">= 1.0"

                  required_providers {
                    aws = {
                      source  = "hashicorp/aws"
                      version = "~> 5.0"
                    }
                  }

                  backend "s3" {
                    bucket = "{{projectNameLower}}-terraform-state"
                    key    = "state/terraform.tfstate"
                    region = var.aws_region
                  }
                }

                provider "aws" {
                  region = var.aws_region
                }

                # Lambda function
                resource "aws_lambda_function" "api" {
                  function_name = "${var.project_name}-api"
                  runtime       = "dotnet8"
                  handler       = "{{options.Name}}.Api"
                  memory_size   = 512
                  timeout       = 30

                  filename         = var.lambda_zip_path
                  source_code_hash = filebase64sha256(var.lambda_zip_path)

                  role = aws_iam_role.lambda_role.arn

                  environment {
                    variables = {
                      ASPNETCORE_ENVIRONMENT = var.environment
                    }
                  }
                }

                # API Gateway
                resource "aws_apigatewayv2_api" "api" {
                  name          = "${var.project_name}-api"
                  protocol_type = "HTTP"
                }

                resource "aws_apigatewayv2_integration" "lambda" {
                  api_id             = aws_apigatewayv2_api.api.id
                  integration_type   = "AWS_PROXY"
                  integration_uri    = aws_lambda_function.api.invoke_arn
                  integration_method = "POST"
                }

                resource "aws_apigatewayv2_route" "default" {
                  api_id    = aws_apigatewayv2_api.api.id
                  route_key = "$default"
                  target    = "integrations/${aws_apigatewayv2_integration.lambda.id}"
                }

                resource "aws_apigatewayv2_stage" "default" {
                  api_id      = aws_apigatewayv2_api.api.id
                  name        = "$default"
                  auto_deploy = true
                }

                # Lambda IAM role
                resource "aws_iam_role" "lambda_role" {
                  name = "${var.project_name}-lambda-role"

                  assume_role_policy = jsonencode({
                    Version = "2012-10-17"
                    Statement = [{
                      Action = "sts:AssumeRole"
                      Effect = "Allow"
                      Principal = {
                        Service = "lambda.amazonaws.com"
                      }
                    }]
                  })
                }

                resource "aws_iam_role_policy_attachment" "lambda_basic" {
                  role       = aws_iam_role.lambda_role.name
                  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
                }
                """;
        }

        return $$"""
            terraform {
              required_version = ">= 1.0"

              required_providers {
                aws = {
                  source  = "hashicorp/aws"
                  version = "~> 5.0"
                }
              }

              backend "s3" {
                bucket = "{{projectNameLower}}-terraform-state"
                key    = "state/terraform.tfstate"
                region = var.aws_region
              }
            }

            provider "aws" {
              region = var.aws_region
            }

            # VPC
            module "vpc" {
              source = "terraform-aws-modules/vpc/aws"

              name = "${var.project_name}-vpc"
              cidr = "10.0.0.0/16"

              azs             = ["${var.aws_region}a", "${var.aws_region}b"]
              private_subnets = ["10.0.1.0/24", "10.0.2.0/24"]
              public_subnets  = ["10.0.101.0/24", "10.0.102.0/24"]

              enable_nat_gateway = true
              single_nat_gateway = var.environment != "prod"
            }

            # RDS PostgreSQL
            module "rds" {
              source = "terraform-aws-modules/rds/aws"

              identifier = "${var.project_name}-db"

              engine            = "postgres"
              engine_version    = "16"
              instance_class    = var.db_instance_class
              allocated_storage = 20

              db_name  = var.project_name
              username = "postgres"
              port     = 5432

              vpc_security_group_ids = [module.vpc.default_security_group_id]
              subnet_ids             = module.vpc.private_subnets

              family = "postgres16"
            }

            # ElastiCache Redis
            module "elasticache" {
              source = "terraform-aws-modules/elasticache/aws"

              cluster_id           = "${var.project_name}-redis"
              engine               = "redis"
              node_type            = var.redis_node_type
              num_cache_nodes      = 1
              parameter_group_name = "default.redis7"

              subnet_ids         = module.vpc.private_subnets
              security_group_ids = [module.vpc.default_security_group_id]
            }

            # ECS Cluster
            module "ecs" {
              source = "terraform-aws-modules/ecs/aws"

              cluster_name = "${var.project_name}-cluster"

              fargate_capacity_providers = {
                FARGATE = {
                  default_capacity_provider_strategy = {
                    weight = 100
                  }
                }
              }
            }
            """;
    }

    public string RenderTerraformVariables(ProjectOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var projectNameLower = _templateParser.NormalizeProjectName(options.Name, NameContext.DockerImage);

        return $$"""
            variable "aws_region" {
              description = "AWS region"
              type        = string
              default     = "us-east-1"
            }

            variable "project_name" {
              description = "Project name"
              type        = string
              default     = "{{projectNameLower}}"
            }

            variable "environment" {
              description = "Environment (dev, staging, prod)"
              type        = string
              default     = "dev"
            }

            variable "db_instance_class" {
              description = "RDS instance class"
              type        = string
              default     = "db.t3.micro"
            }

            variable "redis_node_type" {
              description = "ElastiCache node type"
              type        = string
              default     = "cache.t3.micro"
            }
            {{(options.Architecture == ArchitectureStyle.Serverless ? """

            variable "lambda_zip_path" {
              description = "Path to Lambda deployment package"
              type        = string
              default     = "../publish/api.zip"
            }
            """ : "")}}
            """;
    }

    public string RenderTerraformOutputs(ProjectOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Architecture == ArchitectureStyle.Serverless)
        {
            return """
                output "api_endpoint" {
                  description = "API Gateway endpoint URL"
                  value       = aws_apigatewayv2_api.api.api_endpoint
                }

                output "lambda_function_name" {
                  description = "Lambda function name"
                  value       = aws_lambda_function.api.function_name
                }
                """;
        }

        return """
            output "vpc_id" {
              description = "VPC ID"
              value       = module.vpc.vpc_id
            }

            output "rds_endpoint" {
              description = "RDS endpoint"
              value       = module.rds.db_instance_endpoint
            }

            output "redis_endpoint" {
              description = "ElastiCache endpoint"
              value       = module.elasticache.cluster_address
            }

            output "ecs_cluster_name" {
              description = "ECS cluster name"
              value       = module.ecs.cluster_name
            }
            """;
    }

    public string RenderGitHubActionsCI(ProjectOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var projectNameLower = _templateParser.NormalizeProjectName(options.Name, NameContext.DockerImage);

        return $@"name: CI

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

env:
  DOTNET_VERSION: '10.0.x'

jobs:
  build:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{{{ env.DOTNET_VERSION }}}}

      - name: Restore dependencies
        run: dotnet restore src/{options.Name}.slnx

      - name: Build
        run: dotnet build src/{options.Name}.slnx --no-restore --configuration Release

      - name: Test
        run: dotnet test src/{options.Name}.slnx --no-build --configuration Release --verbosity normal

  docker:
    runs-on: ubuntu-latest
    needs: build
    if: github.event_name == 'push' && github.ref == 'refs/heads/main'

    steps:
      - uses: actions/checkout@v4

      - name: Build Docker image
        run: |
          docker build -t {projectNameLower}:${{{{ github.sha }}}} -f src/{options.Name}.Api/Dockerfile .
";
    }

    #endregion

    #region Module Templates

    public string RenderCatalogModule(ProjectOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return $$"""
            using {{options.Name}}.Catalog.Features.v1.Products;
            using FSH.Framework.Web.Modules;
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Http;
            using Microsoft.AspNetCore.Routing;
            using Microsoft.Extensions.Hosting;

            namespace {{options.Name}}.Catalog;

            public sealed class CatalogModule : IModule
            {
                public void ConfigureServices(IHostApplicationBuilder builder)
                {
                    // Register services
                }

                public void MapEndpoints(IEndpointRouteBuilder endpoints)
                {
                    var group = endpoints.MapGroup("/api/v1/catalog")
                        .WithTags("Catalog");

                    group.MapGetProductsEndpoint();
                }
            }
            """;
    }

    public string RenderCatalogModuleCsproj(ProjectOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return $$"""
            <Project Sdk="Microsoft.NET.Sdk">

              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>

              <ItemGroup>
                <PackageReference Include="FullStackHero.Framework.Core" />
                <PackageReference Include="FullStackHero.Framework.Persistence" />
                <PackageReference Include="FullStackHero.Framework.Web" />
              </ItemGroup>

              <ItemGroup>
                <ProjectReference Include="..\{{options.Name}}.Catalog.Contracts\{{options.Name}}.Catalog.Contracts.csproj" />
              </ItemGroup>

            </Project>
            """;
    }

    public string RenderCatalogContractsCsproj() => _templateLoader.GetStaticTemplate("CatalogContractsCsproj");

    public string RenderGetProductsEndpoint(ProjectOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return $$"""
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Http;
            using Microsoft.AspNetCore.Routing;

            namespace {{options.Name}}.Catalog.Features.v1.Products;

            public static class GetProductsEndpoint
            {
                public static RouteHandlerBuilder MapGetProductsEndpoint(this IEndpointRouteBuilder endpoints)
                {
                    return endpoints.MapGet("/products", () =>
                    {
                        var products = new[]
                        {
                            new { Id = 1, Name = "Product 1", Price = 9.99m },
                            new { Id = 2, Name = "Product 2", Price = 19.99m },
                            new { Id = 3, Name = "Product 3", Price = 29.99m }
                        };

                        return TypedResults.Ok(products);
                    })
                    .WithName("GetProducts")
                    .WithSummary("Get all products")
                    .Produces(StatusCodes.Status200OK);
                }
            }
            """;
    }

    #endregion

    #region Static Content Templates

    public string RenderReadme(ProjectOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var archDescription = options.Architecture switch
        {
            ArchitectureStyle.Monolith => "monolithic",
            ArchitectureStyle.Microservices => "microservices",
            ArchitectureStyle.Serverless => "serverless (AWS Lambda)",
            _ => string.Empty
        };

        return $$"""
            # {{options.Name}}

            A {{archDescription}} application built with [FullStackHero .NET Starter Kit](https://fullstackhero.net).

            ## Getting Started

            ### Prerequisites

            - [.NET 10 SDK](https://dotnet.microsoft.com/download)
            - [Docker](https://www.docker.com/) (optional, for infrastructure)
            {{(options.Database == DatabaseProvider.PostgreSQL ? "- PostgreSQL 16+" : "")}}
            {{(options.Database == DatabaseProvider.SqlServer ? "- SQL Server 2022+" : "")}}
            - Redis

            ### Running the Application

            {{(options.IncludeDocker ? """
            #### Start Infrastructure (Docker)

            ```bash
            docker-compose up -d
            ```
            """ : "")}}

            {{(options.IncludeAspire ? $"""
            #### Run with Aspire

            ```bash
            dotnet run --project src/{options.Name}.AppHost
            ```
            """ : $"""
            #### Run the API

            ```bash
            dotnet run --project src/{options.Name}.Api
            ```
            """)}}

            ### Project Structure

            ```
            src/
            ├── {{options.Name}}.Api/           # Web API project
            ├── {{options.Name}}.Migrations/    # Database migrations
            {{(options.Type == ProjectType.ApiBlazor ? $"├── {options.Name}.Blazor/         # Blazor WebAssembly UI" : "")}}
            {{(options.IncludeAspire ? $"├── {options.Name}.AppHost/        # Aspire orchestrator" : "")}}
            {{(options.IncludeSampleModule ? "└── Modules/                       # Feature modules" : "")}}
            ```

            ## Configuration

            Update `appsettings.json` with your settings:

            - `DatabaseOptions:ConnectionString` - Database connection
            - `CachingOptions:Redis` - Redis connection
            - `JwtOptions:SigningKey` - JWT signing key (change in production!)

            ## License

            MIT
            """;
    }

    public string RenderGitignore() => _templateLoader.GetStaticTemplate("Gitignore");

    public string RenderDirectoryBuildProps(ProjectOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return $$"""
            <Project>
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <LangVersion>latest</LangVersion>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
                <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
                <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
              </PropertyGroup>

              <PropertyGroup>
                <Authors>{{options.Name}}</Authors>
                <Company>{{options.Name}}</Company>
                <Version>1.0.0</Version>
              </PropertyGroup>

              <ItemGroup>
                <PackageReference Include="SonarAnalyzer.CSharp">
                  <PrivateAssets>all</PrivateAssets>
                  <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
                </PackageReference>
              </ItemGroup>
            </Project>
            """;
    }

    public string RenderDirectoryPackagesProps(ProjectOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Use custom version from options, or fall back to framework version
        var version = options.FrameworkVersion ?? _templateLoader.GetFrameworkVersion();

        return $$"""
            <Project>
              <PropertyGroup>
                <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
                <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
              </PropertyGroup>

              <ItemGroup Label="FullStackHero Framework">
                <PackageVersion Include="FullStackHero.Framework.Core" Version="{{version}}" />
                <PackageVersion Include="FullStackHero.Framework.Shared" Version="{{version}}" />
                <PackageVersion Include="FullStackHero.Framework.Persistence" Version="{{version}}" />
                <PackageVersion Include="FullStackHero.Framework.Caching" Version="{{version}}" />
                <PackageVersion Include="FullStackHero.Framework.Mailing" Version="{{version}}" />
                <PackageVersion Include="FullStackHero.Framework.Jobs" Version="{{version}}" />
                <PackageVersion Include="FullStackHero.Framework.Storage" Version="{{version}}" />
                <PackageVersion Include="FullStackHero.Framework.Eventing" Version="{{version}}" />
                <PackageVersion Include="FullStackHero.Framework.Web" Version="{{version}}" />
                <PackageVersion Include="FullStackHero.Framework.Blazor.UI" Version="{{version}}" />
              </ItemGroup>

              <ItemGroup Label="FullStackHero Modules">
                <PackageVersion Include="FullStackHero.Modules.Identity" Version="{{version}}" />
                <PackageVersion Include="FullStackHero.Modules.Identity.Contracts" Version="{{version}}" />
                <PackageVersion Include="FullStackHero.Modules.Multitenancy" Version="{{version}}" />
                <PackageVersion Include="FullStackHero.Modules.Multitenancy.Contracts" Version="{{version}}" />
                <PackageVersion Include="FullStackHero.Modules.Auditing" Version="{{version}}" />
                <PackageVersion Include="FullStackHero.Modules.Auditing.Contracts" Version="{{version}}" />
              </ItemGroup>

              <ItemGroup Label="Aspire">
                <PackageVersion Include="Aspire.Hosting.PostgreSQL" Version="13.1.0" />
                <PackageVersion Include="Aspire.Hosting.SqlServer" Version="13.1.0" />
                <PackageVersion Include="Aspire.Hosting.Redis" Version="13.1.0" />
              </ItemGroup>

              <ItemGroup Label="Database">
                <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.1" />
                <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.1" />
                <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
                <PackageVersion Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.1" />
                <PackageVersion Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.1" />
              </ItemGroup>

              <ItemGroup Label="Blazor">
                <PackageVersion Include="Microsoft.AspNetCore.Components.WebAssembly" Version="10.0.1" />
                <PackageVersion Include="Microsoft.AspNetCore.Components.Authorization" Version="10.0.1" />
                <PackageVersion Include="MudBlazor" Version="8.15.0" />
              </ItemGroup>

              <ItemGroup Label="AWS">
                <PackageVersion Include="AWSSDK.S3" Version="4.0.15.1" />
                <PackageVersion Include="Amazon.Lambda.AspNetCoreServer.Hosting" Version="1.7.2" />
              </ItemGroup>

              <ItemGroup Label="Mediator">
                <PackageVersion Include="Mediator.Abstractions" Version="3.1.0-preview.14" />
                <PackageVersion Include="Mediator.SourceGenerator" Version="3.1.0-preview.14" />
              </ItemGroup>

              <ItemGroup Label="Code Quality">
                <PackageVersion Include="SonarAnalyzer.CSharp" Version="10.17.0.131074" />
              </ItemGroup>
            </Project>
            """;
    }

    public string RenderEditorConfig() => _templateLoader.GetStaticTemplate("EditorConfig");

    public string RenderGlobalJson() => _templateLoader.GetStaticTemplate("GlobalJson");

    #endregion
}