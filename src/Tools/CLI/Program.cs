using FSH.CLI.Commands;
using FSH.CLI.Infrastructure;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("fsh");
    config.SetApplicationVersion(CliVersion.Current);

    // Reject unknown options. Lenient parsing silently drops a typo or the `dotnet new fsh`
    // spelling (`--aspire false`) and scaffolds a different project than the one asked for.
    config.UseStrictParsing();

    config.AddCommand<NewCommand>("new")
        .WithDescription("Create a new FullStackHero .NET project.")
        .WithExample("new", "MyApp")
        .WithExample("new", "MyApp", "--no-aspire", "--no-frontend");

    config.AddCommand<DoctorCommand>("doctor")
        .WithDescription("Check your development environment for required tools.");

    config.AddCommand<InfoCommand>("info")
        .WithDescription("Show CLI and template version information.");

    config.AddCommand<UpdateCommand>("update")
        .WithDescription("Update the FSH CLI tool and dotnet new template to the latest version.");
});

return await app.RunAsync(args).ConfigureAwait(false);
