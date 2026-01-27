using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using FSH.CLI.Models;
using FSH.CLI.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace FSH.CLI.Commands;

/// <summary>
/// Check for and apply FSH framework upgrades.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by Spectre.Console.Cli via reflection")]
internal sealed class UpgradeCommand : AsyncCommand<UpgradeCommand.Settings>
{
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by Spectre.Console.Cli via reflection")]
    internal sealed class Settings : CommandSettings
    {
        [CommandOption("-p|--path")]
        [Description("Path to the FSH project (defaults to current directory)")]
        [DefaultValue(".")]
        public string Path { get; set; } = ".";

        [CommandOption("--check")]
        [Description("Check for available upgrades without applying")]
        [DefaultValue(false)]
        public bool CheckOnly { get; set; }

        [CommandOption("--apply")]
        [Description("Apply available upgrades")]
        [DefaultValue(false)]
        public bool Apply { get; set; }

        [CommandOption("--skip-breaking")]
        [Description("Skip breaking changes during upgrade")]
        [DefaultValue(false)]
        public bool SkipBreaking { get; set; }

        [CommandOption("--force")]
        [Description("Force upgrade even if customizations detected")]
        [DefaultValue(false)]
        public bool Force { get; set; }

        [CommandOption("--dry-run")]
        [Description("Show what would be changed without making modifications")]
        [DefaultValue(false)]
        public bool DryRun { get; set; }

        [CommandOption("--include-prerelease")]
        [Description("Include prerelease versions")]
        [DefaultValue(false)]
        public bool IncludePrerelease { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        // Validate project has manifest
        var manifest = FshManifest.TryLoad(settings.Path);
        if (manifest == null)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] No FSH project found at this location.");
            AnsiConsole.MarkupLine("[dim]This command requires a project created with FSH CLI 10.0.0 or later.[/]");
            AnsiConsole.MarkupLine("[dim]The project must have a [yellow].fsh/manifest.json[/] file.[/]");
            return 1;
        }

        // Show current status
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[blue]FSH Upgrade[/]");
        AnsiConsole.MarkupLine($"[dim]Project:[/] {Path.GetFullPath(settings.Path)}");
        AnsiConsole.MarkupLine($"[dim]Current version:[/] [yellow]{manifest.FshVersion}[/]");
        AnsiConsole.WriteLine();

        // Determine mode
        if (!settings.CheckOnly && !settings.Apply)
        {
            // Default: show help
            ShowUsageHelp();
            return 0;
        }

        if (settings.CheckOnly)
        {
            return await CheckForUpgradesAsync(manifest, settings, cancellationToken);
        }

        if (settings.Apply)
        {
            return await ApplyUpgradesAsync(manifest, settings, cancellationToken);
        }

        return 0;
    }

    private static void ShowUsageHelp()
    {
        AnsiConsole.MarkupLine("[yellow]Usage:[/]");
        AnsiConsole.MarkupLine("  [green]fsh upgrade --check[/]                    Check for available upgrades");
        AnsiConsole.MarkupLine("  [green]fsh upgrade --apply[/]                    Apply available upgrades");
        AnsiConsole.MarkupLine("  [green]fsh upgrade --apply --skip-breaking[/]    Apply safe updates only");
        AnsiConsole.MarkupLine("  [green]fsh upgrade --apply --dry-run[/]          Preview changes without applying");
        AnsiConsole.MarkupLine("  [green]fsh upgrade --check --include-prerelease[/]  Include prereleases");
        AnsiConsole.WriteLine();
    }

    private static async Task<int> CheckForUpgradesAsync(FshManifest manifest, Settings settings, CancellationToken cancellationToken)
    {
        using var githubService = new GitHubReleaseService();

        // Fetch latest release
        GitHubRelease? latestRelease = null;
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Checking for updates...", async ctx =>
            {
                if (settings.IncludePrerelease)
                {
                    // Get all releases and find newest
                    var releases = await githubService.GetReleasesAsync(10, cancellationToken);
                    latestRelease = releases.FirstOrDefault();
                }
                else
                {
                    latestRelease = await githubService.GetLatestReleaseAsync(cancellationToken);
                }
            });

        if (latestRelease == null)
        {
            AnsiConsole.MarkupLine("[yellow]⚠[/] Could not fetch release information from GitHub.");
            AnsiConsole.MarkupLine("[dim]Check your internet connection or try again later.[/]");
            return 1;
        }

        var latestVersion = latestRelease.Version;
        var comparison = VersionComparer.CompareVersions(manifest.FshVersion, latestVersion);

        // Show version comparison
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[blue]Version[/]")
            .AddColumn("[blue]Value[/]");

        table.AddRow("Current", $"[yellow]{manifest.FshVersion}[/]");
        table.AddRow("Latest", comparison < 0 ? $"[green]{latestVersion}[/]" : $"[dim]{latestVersion}[/]");

        if (latestRelease.Prerelease)
        {
            table.AddRow("Type", "[yellow]Prerelease[/]");
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        if (comparison >= 0)
        {
            AnsiConsole.MarkupLine("[green]✓[/] You're up to date!");
            return 0;
        }

        // Fetch package versions for comparison
        AnsiConsole.MarkupLine("[dim]Analyzing changes...[/]");

        var currentPackagesProps = await GetLocalPackagesPropsAsync(settings.Path);
        var latestPackagesProps = await githubService.GetPackagesPropsAsync(latestRelease.TagName, cancellationToken);

        if (currentPackagesProps != null && latestPackagesProps != null)
        {
            var currentVersions = VersionComparer.ParsePackagesProps(currentPackagesProps);
            var latestVersions = VersionComparer.ParsePackagesProps(latestPackagesProps);
            var diff = VersionComparer.Compare(currentVersions, latestVersions);

            if (diff.HasChanges)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[blue]Package Changes:[/]");
                AnsiConsole.WriteLine();

                if (diff.Updated.Count > 0)
                {
                    var updateTable = new Table()
                        .Border(TableBorder.Simple)
                        .AddColumn("Package")
                        .AddColumn("Current")
                        .AddColumn("Latest")
                        .AddColumn("Status");

                    foreach (var update in diff.Updated.OrderBy(u => u.Package))
                    {
                        var status = update.IsBreaking ? "[red]Breaking[/]" : "[green]Safe[/]";
                        updateTable.AddRow(
                            $"[dim]{update.Package}[/]",
                            update.FromVersion,
                            $"[green]{update.ToVersion}[/]",
                            status);
                    }

                    AnsiConsole.Write(updateTable);
                }

                if (diff.Added.Count > 0)
                {
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("[green]New packages:[/]");
                    foreach (var added in diff.Added.OrderBy(a => a.Package))
                    {
                        AnsiConsole.MarkupLine($"  [green]+[/] {added.Package} [dim]({added.Version})[/]");
                    }
                }

                if (diff.Removed.Count > 0)
                {
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("[red]Removed packages:[/]");
                    foreach (var removed in diff.Removed.OrderBy(r => r.Package))
                    {
                        AnsiConsole.MarkupLine($"  [red]-[/] {removed.Package} [dim]({removed.Version})[/]");
                    }
                }

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[dim]Total:[/] {diff.Updated.Count} updates, {diff.Added.Count} new, {diff.Removed.Count} removed");

                if (diff.HasBreakingChanges)
                {
                    AnsiConsole.MarkupLine("[yellow]⚠[/] Some updates may contain breaking changes.");
                }
            }
        }

        // Show release notes summary if available
        if (!string.IsNullOrEmpty(latestRelease.Body))
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[blue]Release Notes:[/]");
            
            var panel = new Panel(TruncateReleaseNotes(latestRelease.Body, 500))
            {
                Border = BoxBorder.Rounded,
                Padding = new Padding(1, 0)
            };
            AnsiConsole.Write(panel);
        }

        // Show next steps
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[dim]Release URL:[/] {latestRelease.HtmlUrl}");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Run [green]fsh upgrade --apply[/] to upgrade.");
        AnsiConsole.MarkupLine("Run [green]fsh upgrade --apply --skip-breaking[/] for safe updates only.");
        AnsiConsole.WriteLine();

        return 0;
    }

    private static async Task<int> ApplyUpgradesAsync(FshManifest manifest, Settings settings, CancellationToken cancellationToken)
    {
        // TODO: Sprint 3 - Implement upgrade apply
        // 1. Fetch latest release
        // 2. Update Directory.Packages.props
        // 3. For code changes, show diff and ask confirmation
        // 4. Update manifest with new versions

        AnsiConsole.MarkupLine("[yellow]⚠ Upgrade apply not yet implemented[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Coming in Sprint 3:[/]");
        AnsiConsole.MarkupLine("[dim]  • Package version updater[/]");
        AnsiConsole.MarkupLine("[dim]  • Safe (non-breaking) auto-apply[/]");
        AnsiConsole.MarkupLine("[dim]  • Interactive diff viewer[/]");
        AnsiConsole.WriteLine();

        if (settings.DryRun)
        {
            AnsiConsole.MarkupLine("[dim]Dry run mode - no changes would be made[/]");
        }

        if (settings.SkipBreaking)
        {
            AnsiConsole.MarkupLine("[dim]Skip breaking mode - would skip breaking changes[/]");
        }

        return 0;
    }

    private static async Task<string?> GetLocalPackagesPropsAsync(string projectPath)
    {
        var packagesPropsPath = Path.Combine(projectPath, "src", "Directory.Packages.props");
        
        if (!File.Exists(packagesPropsPath))
        {
            // Try root
            packagesPropsPath = Path.Combine(projectPath, "Directory.Packages.props");
        }

        if (!File.Exists(packagesPropsPath))
        {
            return null;
        }

        return await File.ReadAllTextAsync(packagesPropsPath);
    }

    private static string TruncateReleaseNotes(string notes, int maxLength)
    {
        if (string.IsNullOrEmpty(notes))
            return string.Empty;

        // Remove markdown links for cleaner display
        notes = System.Text.RegularExpressions.Regex.Replace(notes, @"\[([^\]]+)\]\([^\)]+\)", "$1");
        
        if (notes.Length <= maxLength)
            return notes;

        return notes[..maxLength] + "...";
    }
}
