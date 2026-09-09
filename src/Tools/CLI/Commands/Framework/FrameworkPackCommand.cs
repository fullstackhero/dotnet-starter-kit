using System.ComponentModel;
using FSH.CLI.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace FSH.CLI.Commands.Framework;

/// <summary>
/// Packs the BuildingBlocks projects as <c>FSH.Framework.*</c> NuGet packages and, optionally,
/// publishes them to a local folder feed.
/// </summary>
public sealed class FrameworkPackCommand : AsyncCommand<FrameworkPackCommand.Settings>
{
    /// <summary>Pack flavour. Controls what debugging payload the packages carry.</summary>
    public enum PackProfile
    {
        /// <summary>Embedded PDB + embedded sources; step-into works from a folder feed, offline.</summary>
        Local,

        /// <summary>Separate .snupkg + SourceLink, for a real NuGet server with a symbol server.</summary>
        Public
    }

    public sealed class Settings : CommandSettings
    {
        [Description("Local feed directory to publish into. Defaults to $FSH_LOCAL_FEED, then ~/.fsh/local-nuget.")]
        [CommandOption("-f|--feed")]
        public string? Feed { get; init; }

        [Description("Package version. Defaults to a unique 10.0.0-local.<timestamp>.")]
        [CommandOption("-v|--version")]
        public string? Version { get; init; }

        [Description("Directory to write .nupkg files to. Defaults to <repo>/artifacts/nupkgs.")]
        [CommandOption("-o|--output")]
        public string? Output { get; init; }

        [Description("Pack flavour: local (embedded PDB + sources, default) or public (snupkg + SourceLink).")]
        [CommandOption("-p|--profile")]
        [DefaultValue(PackProfile.Local)]
        public PackProfile Profile { get; init; }

        [Description("Copy the packed .nupkg files into the feed.")]
        [CommandOption("--push")]
        [DefaultValue(false)]
        public bool Push { get; init; }

        [Description("Register the feed as a NuGet source if it is not already registered.")]
        [CommandOption("--register-source")]
        [DefaultValue(false)]
        public bool RegisterSource { get; init; }

        [Description("Purge FSH.Framework.* from the NuGet global-packages cache after packing.")]
        [CommandOption("--clear-cache")]
        [DefaultValue(false)]
        public bool ClearCache { get; init; }

        [Description("Show what would happen without packing anything.")]
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
            AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]'fsh framework' builds packages from BuildingBlocks source, so it must run inside a clone[/]");
            AnsiConsole.MarkupLine($"[{FshConstants.DimColor}](a directory containing both src/BuildingBlocks and .template.config).[/]");
            return 1;
        }

        string version = settings.Version ?? FrameworkFeed.NewLocalVersion();
        string output = settings.Output is { Length: > 0 } o
            ? Path.GetFullPath(o)
            : Path.Combine(repoRoot, "artifacts", "nupkgs");
        string feed = FrameworkFeed.Resolve(settings.Feed);
        bool local = settings.Profile == PackProfile.Local;

        var summary = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey);
        summary.AddColumn("[bold]Setting[/]");
        summary.AddColumn("[bold]Value[/]");
        summary.AddRow("Repository", repoRoot.EscapeMarkup());
        summary.AddRow("Version", $"[{FshConstants.AccentColor}]{version.EscapeMarkup()}[/]");
        summary.AddRow("Profile", local ? "local (embedded PDB + sources)" : "public (snupkg + SourceLink)");
        summary.AddRow("Output", output.EscapeMarkup());
        summary.AddRow("Feed", settings.Push ? feed.EscapeMarkup() : $"[{FshConstants.DimColor}](not pushing)[/]");
        summary.AddRow("Packages", FshConstants.FrameworkProjects.Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
        AnsiConsole.Write(summary);
        AnsiConsole.WriteLine();

        if (settings.DryRun)
        {
            AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]Dry run — nothing was packed.[/]");
            return 0;
        }

        Directory.CreateDirectory(output);

        // Pack leaves first, so a package always exists before the packages that depend on it.
        var failures = new List<string>();
        foreach (string project in FshConstants.FrameworkProjects)
        {
            string projectPath = Path.Combine(repoRoot, "src", "BuildingBlocks", project, $"{project}.csproj");
            if (!File.Exists(projectPath))
            {
                failures.Add($"{project} (project not found)");
                continue;
            }

            string properties =
                $"-p:PackFshFramework=true -p:PackageVersion={version}" +
                (local ? " -p:FshLocalPack=true" : string.Empty);

            // Build and pack as two steps so the build can be forced non-incremental.
            // Switching profiles changes only compiler switches (embedded PDB + embedded
            // sources vs a separate snupkg), which MSBuild's up-to-date check does not treat
            // as a reason to recompile — so an incremental pack after a profile switch would
            // happily ship the previous profile's binary and silently break step-into
            // debugging. `dotnet pack` rejects --no-incremental, hence the separate build.
            // The pack step deliberately does NOT pass --no-build: with an embedded PDB there
            // is no .pdb on disk, and --no-build makes pack demand one (NU5026). Letting pack
            // run the build targets is free here, since the build above just ran.
            var step = await ProcessRunner
                .CaptureWithErrorAsync("dotnet", $"build \"{projectPath}\" -c Release --no-incremental --nologo {properties}",
                                       repoRoot, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (step.exitCode == 0)
            {
                step = await ProcessRunner
                    .CaptureWithErrorAsync("dotnet", $"pack \"{projectPath}\" -c Release --nologo {properties} -o \"{output}\"",
                                           repoRoot, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }

            int exitCode = step.exitCode;

            if (exitCode == 0)
            {
                AnsiConsole.MarkupLine($"  [{FshConstants.SuccessColor}]packed[/] {FshConstants.FrameworkPackagePrefix}{project.EscapeMarkup()}");
            }
            else
            {
                AnsiConsole.MarkupLine($"  [{FshConstants.ErrorColor}]failed[/] {FshConstants.FrameworkPackagePrefix}{project.EscapeMarkup()} (exit code {exitCode})");
                failures.Add(project);

                // Show what dotnet actually said; a bare "failed" leaves nothing to act on.
                IEnumerable<string> diagnostics = $"{step.output}\n{step.error}"
                    .Split('\n')
                    .Where(line => line.Contains("error", StringComparison.OrdinalIgnoreCase))
                    .Take(3);

                foreach (string line in diagnostics)
                    AnsiConsole.MarkupLine($"    [{FshConstants.DimColor}]{line.Trim().EscapeMarkup()}[/]");
            }
        }

        if (failures.Count > 0)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[{FshConstants.ErrorColor}]{failures.Count} package(s) failed to pack.[/]");
            AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]Re-run a single project with 'dotnet pack' to see the full error output.[/]");
            return 1;
        }

        if (settings.Push && !PublishToFeed(output, feed, version))
            return 1;

        if (settings.RegisterSource
            && !await EnsureSourceRegisteredAsync(feed, cancellationToken).ConfigureAwait(false))
        {
            return 1;
        }

        if (settings.ClearCache)
            await FrameworkCacheCleaner.ClearAsync(dryRun: false, cancellationToken).ConfigureAwait(false);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[{FshConstants.SuccessColor}]Done.[/] Consume with:");
        AnsiConsole.MarkupLine($"  [{FshConstants.DimColor}]dotnet build -p:UseFrameworkPackages=true -p:FshFrameworkVersion={version.EscapeMarkup()}[/]");
        AnsiConsole.MarkupLine($"  [{FshConstants.DimColor}]or pin FshFrameworkVersion in the consuming project's Directory.Packages.props[/]");

        return 0;
    }

    /// <summary>
    /// Copies the packages into the feed as a flat folder feed.
    /// </summary>
    /// <remarks>
    /// A plain file copy rather than <c>dotnet nuget push</c>: pushing to a folder source writes
    /// the hierarchical (V3) layout, which would sit awkwardly beside the flat layout most
    /// hand-rolled local feeds already use. NuGet reads a flat folder feed happily, and a copy
    /// is deterministic and trivially inspectable.
    /// </remarks>
    private static bool PublishToFeed(string output, string feed, string version)
    {
        try
        {
            Directory.CreateDirectory(feed);

            int copied = 0;
            foreach (string package in Directory.EnumerateFiles(output, $"{FshConstants.FrameworkPackagePrefix}*.{version}.nupkg"))
            {
                File.Copy(package, Path.Combine(feed, Path.GetFileName(package)), overwrite: true);
                copied++;
            }

            // The public profile emits symbol packages alongside; they belong in the feed too.
            foreach (string symbols in Directory.EnumerateFiles(output, $"{FshConstants.FrameworkPackagePrefix}*.{version}.snupkg"))
            {
                File.Copy(symbols, Path.Combine(feed, Path.GetFileName(symbols)), overwrite: true);
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[{FshConstants.SuccessColor}]Published {copied} package(s)[/] to {feed.EscapeMarkup()}");
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            AnsiConsole.MarkupLine($"[{FshConstants.ErrorColor}]Could not publish to '{feed.EscapeMarkup()}': {ex.Message.EscapeMarkup()}[/]");
            return false;
        }
    }

    private static async Task<bool> EnsureSourceRegisteredAsync(string feed, CancellationToken cancellationToken)
    {
        (bool listed, string sources) = await ProcessRunner
            .CaptureAsync("dotnet", "nuget list source", cancellationToken)
            .ConfigureAwait(false);

        if (listed && sources.Contains(feed, StringComparison.OrdinalIgnoreCase))
        {
            AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]NuGet source already registered.[/]");
            return true;
        }

        int exitCode = await ProcessRunner.RunAsync(
            "dotnet",
            $"nuget add source \"{feed}\" --name {FshConstants.LocalFeedSourceName}",
            showOutput: false,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (exitCode == 0)
        {
            AnsiConsole.MarkupLine($"[{FshConstants.SuccessColor}]Registered NuGet source[/] '{FshConstants.LocalFeedSourceName}'.");
            return true;
        }

        AnsiConsole.MarkupLine($"[{FshConstants.WarningColor}]Could not register the NuGet source (exit code {exitCode}).[/]");
        AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]Add it manually: dotnet nuget add source \"{feed.EscapeMarkup()}\" --name {FshConstants.LocalFeedSourceName}[/]");
        return false;
    }
}
