using System.ComponentModel;
using FSH.CLI.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace FSH.CLI.Commands.Self;

/// <summary>
/// Packs this repository's CLI and installs it as the global <c>fsh</c> tool, so the working
/// copy can be driven with <c>fsh ...</c> instead of <c>dotnet run --project src/Tools/CLI --</c>.
/// </summary>
public sealed class SelfInstallCommand : AsyncCommand<SelfInstallCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [Description("Version to stamp on the locally built tool. Defaults to 10.0.0-local.<timestamp>.")]
        [CommandOption("-v|--version <VERSION>")]
        public string? Version { get; init; }

        [Description("Directory to write the .nupkg to. Defaults to <repo>/artifacts/nupkgs.")]
        [CommandOption("-o|--output <DIR>")]
        public string? Output { get; init; }

        [Description("Show what would happen without packing or installing.")]
        [CommandOption("--dry-run")]
        [DefaultValue(false)]
        public bool DryRun { get; init; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        string? repoRoot = RepoLocator.FindStarterKitRoot();
        if (repoRoot is null)
        {
            AnsiConsole.MarkupLine($"[{FshConstants.ErrorColor}]Not inside a FullStackHero starter-kit repository.[/]");
            AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]'fsh self install' builds the tool from CLI source, so it must run inside a clone.[/]");
            return 1;
        }

        // A distinct prerelease version keeps the locally built tool from being confused with
        // whatever FullStackHero.CLI version is published on nuget.org, and makes each install
        // a different version so `dotnet tool update` never treats it as already current.
        string version = settings.Version ?? FrameworkFeed.NewLocalVersion();
        string output = settings.Output is { Length: > 0 } o
            ? Path.GetFullPath(o)
            : Path.Combine(repoRoot, "artifacts", "nupkgs");
        string projectPath = Path.Combine(repoRoot, "src", "Tools", "CLI", "FSH.CLI.csproj");

        AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]Repository:[/] {repoRoot.EscapeMarkup()}");
        AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]Version:[/]    [{FshConstants.AccentColor}]{version.EscapeMarkup()}[/]");

        if (settings.DryRun)
        {
            AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]Would pack {FshConstants.CliPackageId} into {output.EscapeMarkup()} and install it globally.[/]");
            return 0;
        }

        Directory.CreateDirectory(output);

        var pack = await ProcessRunner.CaptureWithErrorAsync(
            "dotnet",
            // -p:Version too, not just PackageVersion: it feeds AssemblyInformationalVersion,
            // which is what `fsh --version` prints. Without it a locally installed build reports
            // the repo's 10.0.0 and is indistinguishable from the published tool.
            $"pack \"{projectPath}\" -c Release --nologo -p:Version={version} -p:PackageVersion={version} -o \"{output}\"",
            repoRoot, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (pack.exitCode != 0)
        {
            AnsiConsole.MarkupLine($"[{FshConstants.ErrorColor}]Packing the CLI failed.[/]");
            ReportDiagnostics(pack.output, pack.error);
            return 1;
        }

        AnsiConsole.MarkupLine($"  [{FshConstants.SuccessColor}]packed[/] {FshConstants.CliPackageId}");

        // `tool update` installs when absent and upgrades when present, so one call covers both.
        var install = await ProcessRunner.CaptureWithErrorAsync(
            "dotnet",
            $"tool update -g {FshConstants.CliPackageId} --version {version} --add-source \"{output}\"",
            repoRoot, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (install.exitCode != 0)
        {
            AnsiConsole.MarkupLine($"[{FshConstants.ErrorColor}]Installing the global tool failed.[/]");
            ReportDiagnostics(install.output, install.error);
            return 1;
        }

        AnsiConsole.MarkupLine($"  [{FshConstants.SuccessColor}]installed[/] global tool '{FshConstants.ToolCommandName}'");
        AnsiConsole.WriteLine();

        WarnIfToolsDirectoryNotOnPath();

        AnsiConsole.MarkupLine($"[{FshConstants.SuccessColor}]Done.[/] The '{FshConstants.ToolCommandName}' command now runs this working copy:");
        AnsiConsole.MarkupLine($"  [{FshConstants.DimColor}]{FshConstants.ToolCommandName} new MyApp --agents --framework-packages[/]");
        AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]Re-run 'fsh self install' after changing CLI source; 'fsh self uninstall' removes it.[/]");

        return 0;
    }

    private static void ReportDiagnostics(string output, string error)
    {
        IEnumerable<string> lines = $"{output}\n{error}"
            .Split('\n')
            .Where(line => line.Contains("error", StringComparison.OrdinalIgnoreCase))
            .Take(3);

        foreach (string line in lines)
            AnsiConsole.MarkupLine($"    [{FshConstants.DimColor}]{line.Trim().EscapeMarkup()}[/]");
    }

    /// <summary>
    /// A freshly installed global tool is invisible until its directory is on PATH, which is the
    /// usual reason "command not found" follows a successful install.
    /// </summary>
    private static void WarnIfToolsDirectoryNotOnPath()
    {
        string toolsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet", "tools");

        string path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        bool onPath = path
            .Split(Path.PathSeparator)
            .Any(entry => string.Equals(
                entry.TrimEnd(Path.DirectorySeparatorChar),
                toolsDirectory.TrimEnd(Path.DirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase));

        if (onPath) return;

        AnsiConsole.MarkupLine($"[{FshConstants.WarningColor}]{toolsDirectory.EscapeMarkup()} is not on your PATH.[/]");
        AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]Add it to your shell profile, otherwise '{FshConstants.ToolCommandName}' will not be found:[/]");
        AnsiConsole.MarkupLine($"  [{FshConstants.DimColor}]export PATH=\"$PATH:{toolsDirectory.EscapeMarkup()}\"[/]");
        AnsiConsole.WriteLine();
    }
}
