using System.Diagnostics;
using FSH.CLI.Models;
using FSH.CLI.UI;
using Spectre.Console;

namespace FSH.CLI.Scaffolding;

internal static class SolutionGenerator
{
    public static async Task GenerateAsync(ProjectOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        var projectPath = Path.Combine(options.OutputPath, options.Name);

        if (Directory.Exists(projectPath) &&
            Directory.EnumerateFileSystemEntries(projectPath).Any() &&
            !await AnsiConsole.ConfirmAsync($"[dim]Directory[/] [yellow]{projectPath}[/] [dim]exists. Overwrite?[/]", false, cancellationToken))
        {
            AnsiConsole.MarkupLine("[dim]Cancelled.[/]");
            return;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Creating project...[/]");

        // Create directory structure
        ConsoleTheme.WriteStep("Directory structure");
        await CreateDirectoryStructureAsync(projectPath, options);

        // Create solution file
        ConsoleTheme.WriteStep("Solution file");
        await CreateSolutionFileAsync(projectPath, options);

        // Create API project
        ConsoleTheme.WriteStep("API project");
        await CreateApiProjectAsync(projectPath, options);

        // Create Blazor project if needed
        if (options.Type == ProjectType.ApiBlazor)
        {
            ConsoleTheme.WriteStep("Blazor project");
            await CreateBlazorProjectAsync(projectPath, options);
        }

        // Create migrations project
        ConsoleTheme.WriteStep("Migrations project");
        await CreateMigrationsProjectAsync(projectPath, options);

        // Create AppHost if Aspire enabled
        if (options.IncludeAspire)
        {
            ConsoleTheme.WriteStep("Aspire AppHost");
            await CreateAspireAppHostAsync(projectPath, options);
        }

        // Create Docker Compose if enabled
        if (options.IncludeDocker)
        {
            ConsoleTheme.WriteStep("Docker Compose");
            await CreateDockerComposeAsync(projectPath, options);
        }

        // Create sample module if enabled
        if (options.IncludeSampleModule)
        {
            ConsoleTheme.WriteStep("Sample module");
            await CreateSampleModuleAsync(projectPath, options);
        }

        // Create Terraform if enabled
        if (options.IncludeTerraform)
        {
            ConsoleTheme.WriteStep("Terraform");
            await CreateTerraformAsync(projectPath, options);
        }

        // Create GitHub Actions if enabled
        if (options.IncludeGitHubActions)
        {
            ConsoleTheme.WriteStep("GitHub Actions");
            await CreateGitHubActionsAsync(projectPath, options);
        }

        // Create common files
        ConsoleTheme.WriteStep("Common files");
        await CreateCommonFilesAsync(projectPath, options);

        // Create FSH manifest for upgrade tracking
        ConsoleTheme.WriteStep("FSH manifest");
        await CreateFshManifestAsync(projectPath, options);

        // Initialize git repository if enabled
        if (options.InitializeGit)
        {
            ConsoleTheme.WriteStep("Git repository");
            await InitializeGitRepositoryAsync(projectPath);
        }

        // Run dotnet restore
        await RunDotnetRestoreAsync(projectPath, options);

        // Show next steps
        ShowNextSteps(options);
    }

    private static Task CreateDirectoryStructureAsync(string projectPath, ProjectOptions options)
    {
        var directories = new List<string>
        {
            ".fsh",
            "src",
            $"src/{options.Name}.Api",
            $"src/{options.Name}.Api/Properties",
            $"src/{options.Name}.Migrations"
        };

        if (options.Type == ProjectType.ApiBlazor)
        {
            directories.Add($"src/{options.Name}.Blazor");
            directories.Add($"src/{options.Name}.Blazor/Pages");
            directories.Add($"src/{options.Name}.Blazor/Shared");
            directories.Add($"src/{options.Name}.Blazor/wwwroot");
        }

        if (options.IncludeAspire)
        {
            directories.Add($"src/{options.Name}.AppHost");
            directories.Add($"src/{options.Name}.AppHost/Properties");
        }

        if (options.IncludeSampleModule)
        {
            directories.Add($"src/Modules/{options.Name}.Catalog");
            directories.Add($"src/Modules/{options.Name}.Catalog.Contracts");
        }

        if (options.IncludeTerraform)
        {
            directories.Add("terraform");
        }

        if (options.IncludeGitHubActions)
        {
            directories.Add(".github/workflows");
        }

        foreach (var dir in directories)
        {
            Directory.CreateDirectory(Path.Combine(projectPath, dir));
        }

        return Task.CompletedTask;
    }

    private static async Task CreateSolutionFileAsync(string projectPath, ProjectOptions options)
    {
        var slnContent = TemplateEngine.GenerateSolution(options);
        await File.WriteAllTextAsync(Path.Combine(projectPath, "src", $"{options.Name}.slnx"), slnContent);
    }

    private static async Task CreateApiProjectAsync(string projectPath, ProjectOptions options)
    {
        var apiPath = Path.Combine(projectPath, "src", $"{options.Name}.Api");

        // Create .csproj
        var csproj = TemplateEngine.GenerateApiCsproj(options);
        await File.WriteAllTextAsync(Path.Combine(apiPath, $"{options.Name}.Api.csproj"), csproj);

        // Create Program.cs
        var program = TemplateEngine.GenerateApiProgram(options);
        await File.WriteAllTextAsync(Path.Combine(apiPath, "Program.cs"), program);

        // Create appsettings.json
        var appsettings = TemplateEngine.GenerateAppSettings(options);
        await File.WriteAllTextAsync(Path.Combine(apiPath, "appsettings.json"), appsettings);

        // Create appsettings.Development.json
        var appsettingsDev = TemplateEngine.GenerateAppSettingsDevelopment();
        await File.WriteAllTextAsync(Path.Combine(apiPath, "appsettings.Development.json"), appsettingsDev);

        // Create Properties directory and launchSettings.json
        Directory.CreateDirectory(Path.Combine(apiPath, "Properties"));
        var launchSettings = TemplateEngine.GenerateApiLaunchSettings(options);
        await File.WriteAllTextAsync(Path.Combine(apiPath, "Properties", "launchSettings.json"), launchSettings);

        // Create Dockerfile
        var dockerfile = TemplateEngine.GenerateDockerfile(options);
        await File.WriteAllTextAsync(Path.Combine(apiPath, "Dockerfile"), dockerfile);
    }

    private static async Task CreateBlazorProjectAsync(string projectPath, ProjectOptions options)
    {
        var blazorPath = Path.Combine(projectPath, "src", $"{options.Name}.Blazor");

        // Create .csproj
        var csproj = TemplateEngine.GenerateBlazorCsproj();
        await File.WriteAllTextAsync(Path.Combine(blazorPath, $"{options.Name}.Blazor.csproj"), csproj);

        // Create Program.cs
        var program = TemplateEngine.GenerateBlazorProgram(options);
        await File.WriteAllTextAsync(Path.Combine(blazorPath, "Program.cs"), program);

        // Create _Imports.razor
        var imports = TemplateEngine.GenerateBlazorImports(options);
        await File.WriteAllTextAsync(Path.Combine(blazorPath, "_Imports.razor"), imports);

        // Create App.razor
        var app = TemplateEngine.GenerateBlazorApp();
        await File.WriteAllTextAsync(Path.Combine(blazorPath, "App.razor"), app);

        // Create wwwroot directory
        Directory.CreateDirectory(Path.Combine(blazorPath, "wwwroot"));

        // Create Shared directory and MainLayout.razor
        Directory.CreateDirectory(Path.Combine(blazorPath, "Shared"));
        var mainLayout = TemplateEngine.GenerateBlazorMainLayout(options);
        await File.WriteAllTextAsync(Path.Combine(blazorPath, "Shared", "MainLayout.razor"), mainLayout);

        // Create Pages directory
        Directory.CreateDirectory(Path.Combine(blazorPath, "Pages"));

        // Create Index.razor
        var index = TemplateEngine.GenerateBlazorIndexPage(options);
        await File.WriteAllTextAsync(Path.Combine(blazorPath, "Pages", "Index.razor"), index);
    }

    private static async Task CreateMigrationsProjectAsync(string projectPath, ProjectOptions options)
    {
        var migrationsPath = Path.Combine(projectPath, "src", $"{options.Name}.Migrations");

        // Create .csproj
        var csproj = TemplateEngine.GenerateMigrationsCsproj(options);
        await File.WriteAllTextAsync(Path.Combine(migrationsPath, $"{options.Name}.Migrations.csproj"), csproj);
    }

    private static async Task CreateAspireAppHostAsync(string projectPath, ProjectOptions options)
    {
        var appHostPath = Path.Combine(projectPath, "src", $"{options.Name}.AppHost");

        // Create .csproj
        var csproj = TemplateEngine.GenerateAppHostCsproj(options);
        await File.WriteAllTextAsync(Path.Combine(appHostPath, $"{options.Name}.AppHost.csproj"), csproj);

        // Create Program.cs
        var program = TemplateEngine.GenerateAppHostProgram(options);
        await File.WriteAllTextAsync(Path.Combine(appHostPath, "Program.cs"), program);

        // Create Properties directory and launchSettings.json
        Directory.CreateDirectory(Path.Combine(appHostPath, "Properties"));
        var launchSettings = TemplateEngine.GenerateAppHostLaunchSettings(options);
        await File.WriteAllTextAsync(Path.Combine(appHostPath, "Properties", "launchSettings.json"), launchSettings);
    }

    private static async Task CreateDockerComposeAsync(string projectPath, ProjectOptions options)
    {
        var dockerCompose = TemplateEngine.GenerateDockerCompose(options);
        await File.WriteAllTextAsync(Path.Combine(projectPath, "docker-compose.yml"), dockerCompose);

        var dockerComposeOverride = TemplateEngine.GenerateDockerComposeOverride();
        await File.WriteAllTextAsync(Path.Combine(projectPath, "docker-compose.override.yml"), dockerComposeOverride);
    }

    private static async Task CreateSampleModuleAsync(string projectPath, ProjectOptions options)
    {
        var modulePath = Path.Combine(projectPath, "src", "Modules", $"{options.Name}.Catalog");
        var contractsPath = Path.Combine(projectPath, "src", "Modules", $"{options.Name}.Catalog.Contracts");

        // Create Contracts project
        var contractsCsproj = TemplateEngine.GenerateCatalogContractsCsproj();
        await File.WriteAllTextAsync(Path.Combine(contractsPath, $"{options.Name}.Catalog.Contracts.csproj"), contractsCsproj);

        // Create Module project
        var moduleCsproj = TemplateEngine.GenerateCatalogModuleCsproj(options);
        await File.WriteAllTextAsync(Path.Combine(modulePath, $"{options.Name}.Catalog.csproj"), moduleCsproj);

        // Create CatalogModule.cs
        var catalogModule = TemplateEngine.GenerateCatalogModule(options);
        Directory.CreateDirectory(modulePath);
        await File.WriteAllTextAsync(Path.Combine(modulePath, "CatalogModule.cs"), catalogModule);

        // Create Features directory with sample endpoint
        var featuresPath = Path.Combine(modulePath, "Features", "v1", "Products");
        Directory.CreateDirectory(featuresPath);

        var getProducts = TemplateEngine.GenerateGetProductsEndpoint(options);
        await File.WriteAllTextAsync(Path.Combine(featuresPath, "GetProductsEndpoint.cs"), getProducts);
    }

    private static async Task CreateTerraformAsync(string projectPath, ProjectOptions options)
    {
        var terraformPath = Path.Combine(projectPath, "terraform");

        var mainTf = TemplateEngine.GenerateTerraformMain(options);
        await File.WriteAllTextAsync(Path.Combine(terraformPath, "main.tf"), mainTf);

        var variablesTf = TemplateEngine.GenerateTerraformVariables(options);
        await File.WriteAllTextAsync(Path.Combine(terraformPath, "variables.tf"), variablesTf);

        var outputsTf = TemplateEngine.GenerateTerraformOutputs(options);
        await File.WriteAllTextAsync(Path.Combine(terraformPath, "outputs.tf"), outputsTf);
    }

    private static async Task CreateGitHubActionsAsync(string projectPath, ProjectOptions options)
    {
        var workflowsPath = Path.Combine(projectPath, ".github", "workflows");

        var ciYaml = TemplateEngine.GenerateGitHubActionsCI(options);
        await File.WriteAllTextAsync(Path.Combine(workflowsPath, "ci.yml"), ciYaml);
    }

    private static async Task CreateCommonFilesAsync(string projectPath, ProjectOptions options)
    {
        // Create .gitignore
        var gitignore = TemplateEngine.GenerateGitignore();
        await File.WriteAllTextAsync(Path.Combine(projectPath, ".gitignore"), gitignore);

        // Create .editorconfig
        var editorconfig = TemplateEngine.GenerateEditorConfig();
        await File.WriteAllTextAsync(Path.Combine(projectPath, ".editorconfig"), editorconfig);

        // Create global.json
        var globalJson = TemplateEngine.GenerateGlobalJson();
        await File.WriteAllTextAsync(Path.Combine(projectPath, "global.json"), globalJson);

        // Create Directory.Build.props
        var buildProps = TemplateEngine.GenerateDirectoryBuildProps(options);
        await File.WriteAllTextAsync(Path.Combine(projectPath, "src", "Directory.Build.props"), buildProps);

        // Create Directory.Packages.props
        var packagesProps = TemplateEngine.GenerateDirectoryPackagesProps(options);
        await File.WriteAllTextAsync(Path.Combine(projectPath, "src", "Directory.Packages.props"), packagesProps);

        // Create README.md
        var readme = TemplateEngine.GenerateReadme(options);
        await File.WriteAllTextAsync(Path.Combine(projectPath, "README.md"), readme);
    }

    private static async Task InitializeGitRepositoryAsync(string projectPath)
    {
        // Run git init
        using var initProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "init",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = projectPath
            }
        };

        try
        {
            initProcess.Start();
            await initProcess.WaitForExitAsync();

            if (initProcess.ExitCode != 0)
            {
                var error = await initProcess.StandardError.ReadToEndAsync();
                ConsoleTheme.WriteWarning($"git init failed: {error}");
                return;
            }

            // Run dotnet new gitignore to get a comprehensive .NET gitignore
            using var gitignoreProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "new gitignore --force",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = projectPath
                }
            };

            gitignoreProcess.Start();
            await gitignoreProcess.WaitForExitAsync();

            // If dotnet new gitignore fails, we already have our basic .gitignore so it's fine
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            // Git not installed or not in PATH
            ConsoleTheme.WriteWarning($"Could not initialize git repository (is git installed?): {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            ConsoleTheme.WriteWarning($"Could not initialize git repository: {ex.Message}");
        }
    }

    private static async Task RunDotnetRestoreAsync(string projectPath, ProjectOptions options)
    {
        ConsoleTheme.WriteStep("Restoring packages");

        var slnPath = Path.Combine(projectPath, "src", $"{options.Name}.slnx");

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"restore \"{slnPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = projectPath
            }
        };

        process.Start();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            ConsoleTheme.WriteWarning($"Restore warnings: {error}");
        }
    }

    private static Task CreateFshManifestAsync(string projectPath, ProjectOptions options)
    {
        var cliVersion = GetCliVersion();
        var fshVersion = options.FrameworkVersion ?? cliVersion;

        // Determine which modules are included
        var modules = new List<string> { "identity", "multitenancy", "auditing" };
        if (options.IncludeSampleModule)
        {
            modules.Add("catalog");
        }

        var manifest = new FshManifest
        {
            FshVersion = fshVersion,
            CreatedAt = DateTimeOffset.UtcNow,
            CliVersion = cliVersion,
            Options = new FshManifestOptions
            {
                Type = options.Type.ToString().ToLowerInvariant(),
                Architecture = options.Architecture.ToString().ToLowerInvariant(),
                Database = options.Database.ToString().ToLowerInvariant(),
                Modules = modules,
                IncludeDocker = options.IncludeDocker,
                IncludeAspire = options.IncludeAspire,
                IncludeTerraform = options.IncludeTerraform,
                IncludeGitHubActions = options.IncludeGitHubActions
            },
            Tracking = new FshManifestTracking
            {
                BuildingBlocks = new Dictionary<string, string>
                {
                    ["Core"] = fshVersion,
                    ["Web"] = fshVersion,
                    ["Persistence"] = fshVersion,
                    ["Infrastructure"] = fshVersion
                },
                Customizations = []
            }
        };

        manifest.Save(projectPath);
        return Task.CompletedTask;
    }

    private static string GetCliVersion()
    {
        var assembly = typeof(SolutionGenerator).Assembly;
        var version = assembly.GetName().Version;
        return version?.ToString(3) ?? "10.0.0";
    }

    private static void ShowNextSteps(ProjectOptions options)
    {
        ConsoleTheme.WriteDone($"Created [bold]{options.Name}[/]");

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Get started:[/]");
        AnsiConsole.MarkupLine($"  cd {options.Name}");

        if (options.IncludeAspire)
        {
            AnsiConsole.MarkupLine($"  dotnet run --project src/{options.Name}.AppHost");
        }
        else
        {
            AnsiConsole.MarkupLine($"  dotnet run --project src/{options.Name}.Api");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Docs:[/] https://fullstackhero.net");
        AnsiConsole.WriteLine();
    }
}
