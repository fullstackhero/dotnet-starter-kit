using FSH.CLI.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace FSH.CLI.Commands.Self;

/// <summary>
/// Removes the globally installed <c>fsh</c> tool, whether it came from nuget.org or from
/// <c>fsh self install</c>.
/// </summary>
public sealed class SelfUninstallCommand : AsyncCommand
{
    protected override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        var result = await ProcessRunner.CaptureWithErrorAsync(
            "dotnet", $"tool uninstall -g {FshConstants.CliPackageId}",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result.exitCode != 0)
        {
            AnsiConsole.MarkupLine($"[{FshConstants.WarningColor}]Could not uninstall the global tool.[/]");
            AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]It may not be installed: dotnet tool list -g[/]");
            return 1;
        }

        AnsiConsole.MarkupLine($"[{FshConstants.SuccessColor}]Removed[/] the global '{FshConstants.ToolCommandName}' tool.");
        AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]To go back to the published build: dotnet tool install -g {FshConstants.CliPackageId}[/]");
        return 0;
    }
}
