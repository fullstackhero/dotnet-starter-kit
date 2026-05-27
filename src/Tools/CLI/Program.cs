using System.Reflection;
using FSH.CLI.Commands;
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

    config.AddCommand<NewCommand>("new")
        .WithDescription("Create a new FullStackHero .NET project.")
        .WithExample("new", "MyApp")
        .WithExample("new", "MyApp", "--db", "sqlserver")
        .WithExample("new", "MyApp", "--no-aspire", "--no-git");

    config.AddCommand<DoctorCommand>("doctor")
        .WithDescription("Check your development environment for required tools.");

    config.AddCommand<InfoCommand>("info")
        .WithDescription("Show CLI and template version information.");

    config.AddCommand<UpdateCommand>("update")
        .WithDescription("Update the FSH CLI tool and dotnet new template to the latest version.");
});

return await app.RunAsync(args).ConfigureAwait(false);
