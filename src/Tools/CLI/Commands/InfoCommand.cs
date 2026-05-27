using System.Reflection;
using FSH.CLI.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace FSH.CLI.Commands;

public sealed class InfoCommand : AsyncCommand
{
    protected override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        string currentVersion = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)
            ?? "unknown";

        AnsiConsole.MarkupLine($"[bold {FshConstants.AccentColor}]FullStackHero .NET Starter Kit CLI[/]");
        AnsiConsole.WriteLine();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .HideHeaders()
            .AddColumn("Key")
            .AddColumn("Value");

        table.AddRow("CLI Version", $"[bold]{currentVersion.EscapeMarkup()}[/]");

        // Run NuGet + template checks in parallel
        Task<string?> latestCliTask = NuGetClient.GetLatestVersionAsync(FshConstants.CliPackageId, cancellationToken);
        Task<string?> templateVersionTask = NuGetClient.GetInstalledTemplateVersionAsync(cancellationToken);
        Task<string?> latestTemplateTask = NuGetClient.GetLatestVersionAsync(FshConstants.TemplatePackageId, cancellationToken);
        Task<(bool, string)> sdkTask = ProcessRunner.CaptureAsync("dotnet", "--version", cancellationToken);

        await Task.WhenAll(latestCliTask, templateVersionTask, latestTemplateTask, sdkTask).ConfigureAwait(false);

        string? latestCli = await latestCliTask.ConfigureAwait(false);
        string? templateVersion = await templateVersionTask.ConfigureAwait(false);
        string? latestTemplate = await latestTemplateTask.ConfigureAwait(false);
        (bool sdkOk, string sdkVersion) = await sdkTask.ConfigureAwait(false);

        // Latest CLI
        if (latestCli is not null)
        {
            bool isUpToDate = latestCli == currentVersion;
            table.AddRow("Latest CLI", isUpToDate
                ? $"[{FshConstants.SuccessColor}]{latestCli} (up to date)[/]"
                : $"[{FshConstants.WarningColor}]{latestCli} (update available — run 'fsh update')[/]");
        }
        else
        {
            table.AddRow("Latest CLI", $"[{FshConstants.DimColor}]could not check[/]");
        }

        // Template version
        table.AddRow("Template", templateVersion is not null
            ? $"v{templateVersion}"
            : $"[{FshConstants.DimColor}]not installed[/]");

        if (latestTemplate is not null)
        {
            bool isUpToDate = latestTemplate == templateVersion;
            table.AddRow("Latest Template", isUpToDate
                ? $"[{FshConstants.SuccessColor}]{latestTemplate} (up to date)[/]"
                : $"[{FshConstants.WarningColor}]{latestTemplate} (update available)[/]");
        }

        // .NET SDK
        if (sdkOk)
        {
            table.AddRow(".NET SDK", $"v{sdkVersion}");
        }

        // Links
        table.AddRow("Documentation", $"[link={FshConstants.DocsUrl}]{FshConstants.DocsUrl}[/]");
        table.AddRow("Release Notes", $"[link={FshConstants.ReleaseNotesUrl}]{FshConstants.ReleaseNotesUrl}[/]");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        return 0;
    }
}
