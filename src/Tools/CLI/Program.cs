using System.Reflection;
using FSH.CLI.Commands;
using FSH.CLI.Commands.Framework;
using FSH.CLI.Commands.Self;
using Spectre.Console.Cli;

// Strip the +gitsha build-metadata suffix so `fsh --version` prints a clean version.
var cliVersion = Assembly.GetExecutingAssembly()
    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion?.Split('+')[0]
    ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)
    ?? "unknown";

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("fsh");
    config.SetApplicationVersion(cliVersion);

    // Fail on unknown options instead of collecting them into Remaining. Without this a
    // mistyped or template-style flag (--frameworkPackages instead of --framework-packages)
    // is silently discarded, and the user gets a project that quietly ignores what they asked
    // for — far worse than an error.
    config.UseStrictParsing();

    config.AddCommand<NewCommand>("new")
        .WithDescription("Create a new FullStackHero .NET project.")
        .WithExample("new", "MyApp")
        .WithExample("new", "MyApp", "--no-aspire", "--no-frontend");

    config.AddCommand<DoctorCommand>("doctor")
        .WithDescription("Check your development environment for required tools.");

    config.AddCommand<InfoCommand>("info")
        .WithDescription("Show CLI and template version information.");

    config.AddCommand<UpgradeCommand>("upgrade")
        .WithDescription("Update an existing project to the latest template, as a reviewable git merge.")
        .WithExample("upgrade", "--dry-run")
        .WithExample("upgrade", "--project", "../my-app", "--merge");

    config.AddCommand<UpdateCommand>("update")
        .WithDescription("Update the FSH CLI tool and dotnet new template to the latest version.");

    // Build the CLI from this working copy and install it as the global `fsh` tool, so the
    // rest of these commands can be typed as `fsh ...` instead of `dotnet run --project ... --`.
    config.AddBranch("self", self =>
    {
        self.SetDescription("Manage the globally installed fsh tool built from this repository.");

        self.AddCommand<SelfInstallCommand>("install")
            .WithDescription("Pack this repository's CLI and install it as the global 'fsh' tool.")
            .WithExample("self", "install");

        self.AddCommand<SelfUninstallCommand>("uninstall")
            .WithDescription("Remove the globally installed 'fsh' tool.");
    });

    // Maintainer commands: they build FSH.Framework.* packages from BuildingBlocks source,
    // so they only run inside a starter-kit clone (see RepoLocator).
    config.AddBranch("framework", framework =>
    {
        framework.SetDescription("Build and publish the FSH.Framework.* packages (opt-in framework packaging).");

        framework.AddCommand<FrameworkPackCommand>("pack")
            .WithDescription("Pack the BuildingBlocks projects and optionally publish them to a local feed.")
            .WithExample("framework", "pack", "--push", "--clear-cache")
            .WithExample("framework", "pack", "--feed", "/path/to/feed", "--register-source");

        framework.AddCommand<FrameworkListCommand>("list")
            .WithDescription("List the FSH.Framework.* packages available in the local feed.");

        framework.AddCommand<FrameworkSwapCommand>("swap")
            .WithDescription("Switch an existing project between owned kernel source and framework packages.")
            .WithExample("framework", "swap", "--to", "source")
            .WithExample("framework", "swap", "--to", "packages", "--project", "../my-app");

        framework.AddCommand<FrameworkCleanCacheCommand>("clean-cache")
            .WithDescription("Purge FSH.Framework.* from the NuGet global-packages cache.");
    });
});

return await app.RunAsync(args).ConfigureAwait(false);
