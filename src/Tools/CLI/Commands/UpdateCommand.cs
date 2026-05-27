using FSH.CLI.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace FSH.CLI.Commands;

public sealed class UpdateCommand : AsyncCommand
{
    protected override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine($"[bold {FshConstants.AccentColor}]Updating FSH tools...[/]");
        AnsiConsole.WriteLine();

        bool cliSuccess = await RunUpdateStepAsync(
            "Updating FSH CLI",
            "dotnet", $"tool update -g {FshConstants.CliPackageId}",
            cancellationToken).ConfigureAwait(false);

        bool templateSuccess = await RunUpdateStepAsync(
            "Updating FSH template",
            "dotnet", $"new install {FshConstants.TemplatePackageId}",
            cancellationToken).ConfigureAwait(false);

        AnsiConsole.WriteLine();

        bool allSuccess = cliSuccess && templateSuccess;
        if (allSuccess)
        {
            AnsiConsole.MarkupLine($"[{FshConstants.SuccessColor}]All updates complete.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[{FshConstants.WarningColor}]Some updates failed. Check the output above.[/]");
        }

        return allSuccess ? 0 : 1;
    }

    private static async Task<bool> RunUpdateStepAsync(
        string description, string command, string arguments, CancellationToken cancellationToken)
    {
        return await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse(FshConstants.AccentColor))
            .StartAsync(description, async _ =>
            {
                int exitCode = await ProcessRunner.RunAsync(
                    command, arguments,
                    showOutput: false,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                if (exitCode == 0)
                {
                    AnsiConsole.MarkupLine($"  [{FshConstants.SuccessColor}]{description}: done[/]");
                    return true;
                }

                AnsiConsole.MarkupLine($"  [{FshConstants.ErrorColor}]{description}: failed (exit code {exitCode})[/]");
                return false;
            }).ConfigureAwait(false);
    }
}
