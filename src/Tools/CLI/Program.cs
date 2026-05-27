using FSH.CLI.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("fsh");

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
