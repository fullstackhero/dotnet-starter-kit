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
        using var githubService = new GitHubReleaseService();

        // Fetch latest release
        GitHubRelease? latestRelease = null;
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Fetching latest release...", async ctx =>
            {
                if (settings.IncludePrerelease)
                {
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
            AnsiConsole.MarkupLine("[red]Error:[/] Could not fetch release information from GitHub.");
            return 1;
        }

        var latestVersion = latestRelease.Version;
        var comparison = VersionComparer.CompareVersions(manifest.FshVersion, latestVersion);

        if (comparison >= 0)
        {
            AnsiConsole.MarkupLine("[green]✓[/] Already up to date!");
            return 0;
        }

        AnsiConsole.MarkupLine($"[dim]Upgrading:[/] [yellow]{manifest.FshVersion}[/] → [green]{latestVersion}[/]");
        AnsiConsole.WriteLine();

        // Get package diff
        var currentPackagesProps = await GetLocalPackagesPropsAsync(settings.Path);
        var latestPackagesProps = await githubService.GetPackagesPropsAsync(latestRelease.TagName, cancellationToken);

        if (currentPackagesProps == null)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Could not read Directory.Packages.props");
            return 1;
        }

        if (latestPackagesProps == null)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Could not fetch latest Directory.Packages.props from GitHub");
            return 1;
        }

        var currentVersions = VersionComparer.ParsePackagesProps(currentPackagesProps);
        var latestVersions = VersionComparer.ParsePackagesProps(latestPackagesProps);
        var diff = VersionComparer.Compare(currentVersions, latestVersions);

        if (!diff.HasChanges)
        {
            AnsiConsole.MarkupLine("[dim]No package changes detected.[/]");
            
            // Still update manifest version
            if (!settings.DryRun)
            {
                await PackageUpdater.UpdateManifestAsync(settings.Path, latestVersion, cancellationToken);
                AnsiConsole.MarkupLine("[green]✓[/] Updated manifest version.");
            }
            return 0;
        }

        // Show what will be changed
        AnsiConsole.MarkupLine("[blue]Changes to apply:[/]");
        AnsiConsole.WriteLine();

        if (diff.Updated.Count > 0)
        {
            var updateTable = new Table()
                .Border(TableBorder.Simple)
                .AddColumn("Package")
                .AddColumn("From")
                .AddColumn("To")
                .AddColumn("Status");

            foreach (var update in diff.Updated.OrderBy(u => u.Package))
            {
                var willSkip = settings.SkipBreaking && update.IsBreaking;
                
                string status;
                if (!update.IsBreaking)
                    status = "[green]Safe[/]";
                else if (willSkip)
                    status = "[yellow]Skip (breaking)[/]";
                else
                    status = "[red]Breaking[/]";

                var packageName = willSkip ? $"[strikethrough dim]{update.Package}[/]" : update.Package;
                    
                updateTable.AddRow(
                    packageName,
                    update.FromVersion,
                    $"[green]{update.ToVersion}[/]",
                    status);
            }

            AnsiConsole.Write(updateTable);
        }

        if (diff.Added.Count > 0)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[green]+[/] {diff.Added.Count} new packages will be added");
        }

        if (diff.Removed.Count > 0)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[yellow]![/] {diff.Removed.Count} packages are no longer in the latest release (manual review needed)");
        }

        AnsiConsole.WriteLine();

        // Dry run mode - stop here
        if (settings.DryRun)
        {
            AnsiConsole.MarkupLine("[yellow]Dry run mode[/] - no changes were made.");
            return 0;
        }

        // Confirm unless forced
        if (!settings.Force)
        {
            var confirm = await AnsiConsole.ConfirmAsync("Apply these changes?", false, cancellationToken);
            if (!confirm)
            {
                AnsiConsole.MarkupLine("[dim]Cancelled.[/]");
                return 0;
            }
        }

        // Create backup
        AnsiConsole.MarkupLine("[dim]Creating backup...[/]");
        var backupPath = await PackageUpdater.CreateBackupAsync(settings.Path, cancellationToken);

        if (backupPath == null)
        {
            AnsiConsole.MarkupLine("[yellow]⚠[/] Could not create backup. Continue anyway?");
            if (!settings.Force)
            {
                var continueAnyway = await AnsiConsole.ConfirmAsync("Continue without backup?", false, cancellationToken);
                if (!continueAnyway)
                {
                    AnsiConsole.MarkupLine("[dim]Cancelled.[/]");
                    return 0;
                }
            }
        }
        else
        {
            AnsiConsole.MarkupLine($"[dim]Backup created:[/] {backupPath}");
        }

        // Apply updates
        var updateOptions = new UpdateOptions
        {
            DryRun = false,
            SkipBreaking = settings.SkipBreaking,
            Force = settings.Force
        };

        UpdateResult result;
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Applying updates...", async ctx =>
            {
                result = await PackageUpdater.UpdatePackagesPropsAsync(
                    settings.Path,
                    diff,
                    updateOptions,
                    cancellationToken);
            });

        result = await PackageUpdater.UpdatePackagesPropsAsync(
            settings.Path,
            diff,
            updateOptions,
            cancellationToken);

        // Show results
        AnsiConsole.WriteLine();

        if (result.Success)
        {
            AnsiConsole.MarkupLine("[green]✓[/] Packages updated successfully!");
            AnsiConsole.WriteLine();

            if (result.Updated.Count > 0)
            {
                AnsiConsole.MarkupLine($"[green]Updated:[/] {result.Updated.Count} packages");
            }

            if (result.Added.Count > 0)
            {
                AnsiConsole.MarkupLine($"[green]Added:[/] {result.Added.Count} packages");
            }

            if (result.Skipped.Count > 0)
            {
                AnsiConsole.MarkupLine($"[yellow]Skipped:[/] {result.Skipped.Count} packages (breaking changes)");
            }

            // Update manifest
            var manifestUpdated = await PackageUpdater.UpdateManifestAsync(settings.Path, latestVersion, cancellationToken);
            if (manifestUpdated)
            {
                AnsiConsole.MarkupLine("[green]✓[/] Manifest updated.");
            }

            // Show warnings
            if (result.Warnings.Count > 0)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[yellow]Warnings:[/]");
                foreach (var warning in result.Warnings)
                {
                    AnsiConsole.MarkupLine($"  [yellow]![/] {warning}");
                }
            }

            // Next steps
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Next steps:[/]");
            AnsiConsole.MarkupLine("  1. Run [green]dotnet restore[/] to restore packages");
            AnsiConsole.MarkupLine("  2. Run [green]dotnet build[/] to verify the upgrade");
            AnsiConsole.MarkupLine("  3. Review and test your application");

            if (backupPath != null)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[dim]To rollback:[/] restore from {backupPath}");
            }
        }
        else
        {
            AnsiConsole.MarkupLine("[red]✗[/] Upgrade failed!");

            foreach (var error in result.Errors)
            {
                AnsiConsole.MarkupLine($"  [red]Error:[/] {error}");
            }

            // Offer to restore backup
            if (backupPath != null)
            {
                AnsiConsole.WriteLine();
                var restore = await AnsiConsole.ConfirmAsync("Restore from backup?", true, cancellationToken);
                if (restore)
                {
                    var restored = await PackageUpdater.RestoreBackupAsync(backupPath, cancellationToken);
                    if (restored)
                    {
                        AnsiConsole.MarkupLine("[green]✓[/] Restored from backup.");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]✗[/] Could not restore. Manual restore needed from: {backupPath}");
                    }
                }
            }

            return 1;
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
