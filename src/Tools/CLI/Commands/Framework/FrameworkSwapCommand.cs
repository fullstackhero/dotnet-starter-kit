using System.ComponentModel;
using FSH.CLI.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace FSH.CLI.Commands.Framework;

/// <summary>
/// Switches an existing scaffolded project between owning the kernel as source and consuming it
/// as <c>FSH.Framework.*</c> packages.
/// </summary>
public sealed class FrameworkSwapCommand : AsyncCommand<FrameworkSwapCommand.Settings>
{
    public enum SwapTarget
    {
        /// <summary>Own <c>src/BuildingBlocks</c> as source.</summary>
        Source,

        /// <summary>Consume the kernel from a NuGet feed.</summary>
        Packages
    }

    public sealed class Settings : CommandSettings
    {
        [Description("Which side to swap to: source or packages.")]
        [CommandOption("-t|--to <TARGET>")]
        public SwapTarget To { get; init; }

        [Description("Project directory to convert. Defaults to the current directory.")]
        [CommandOption("--project <PATH>")]
        public string? Project { get; init; }

        [Description("Starter-kit checkout or template to take BuildingBlocks from. Env: FSH_TEMPLATE_PATH.")]
        [CommandOption("--from <PATH>")]
        public string? From { get; init; }

        [Description("Feed serving the FSH.Framework.* packages. Env: FSH_LOCAL_FEED.")]
        [CommandOption("-f|--feed <PATH>")]
        public string? Feed { get; init; }

        [Description("Package version to pin. Defaults to the newest in the feed.")]
        [CommandOption("-v|--version <VERSION>")]
        public string? Version { get; init; }

        [Description("Skip the confirmation prompt before deleting kernel source.")]
        [CommandOption("-y|--yes")]
        [DefaultValue(false)]
        public bool Yes { get; init; }

        [Description("Show what would change without touching anything.")]
        [CommandOption("--dry-run")]
        [DefaultValue(false)]
        public bool DryRun { get; init; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        ScaffoldedProject? project = ScaffoldedProject.Locate(settings.Project);
        if (project is null)
        {
            AnsiConsole.MarkupLine($"[{FshConstants.ErrorColor}]No scaffolded project found here.[/]");
            AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]Run this inside a project created by 'fsh new' (a directory with a single src/*.slnx),[/]");
            AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]or point at one with --project <path>.[/]");
            return 1;
        }

        AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]Project:[/] {project.Name.EscapeMarkup()} [{FshConstants.DimColor}]({project.Root.EscapeMarkup()})[/]");

        return settings.To == SwapTarget.Source
            ? await SwapToSourceAsync(project, settings, cancellationToken).ConfigureAwait(false)
            : SwapToPackages(project, settings);
    }

    /// <summary>
    /// Brings <c>src/BuildingBlocks</c> back into the project.
    /// </summary>
    /// <remarks>
    /// The kernel is regenerated from the template rather than copied out of a starter-kit
    /// clone, because the template rewrites tokens inside BuildingBlocks — including functional
    /// ones such as <c>MultitenancyConstants.Issuer</c>. A raw copy would silently install the
    /// starter kit's own issuer and brand strings into someone else's project.
    /// </remarks>
    private static async Task<int> SwapToSourceAsync(ScaffoldedProject project, Settings settings, CancellationToken cancellationToken)
    {
        if (project.HasBuildingBlocksSource)
        {
            AnsiConsole.MarkupLine($"[{FshConstants.SuccessColor}]Already on owned source[/] — src/BuildingBlocks is present. Nothing to do.");
            return 0;
        }

        if (settings.DryRun)
        {
            AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]Would generate src/BuildingBlocks (+ src/Tests/Framework.Tests) from the template,[/]");
            AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]add them to {Path.GetFileName(project.SolutionPath).EscapeMarkup()}, and drop the local feed from NuGet.config.[/]");
            return 0;
        }

        if (!await TemplateInstaller.EnsureInstalledAsync(
                settings.From, templateVersion: null, templateSource: null,
                refresh: settings.From is not null, cancellationToken).ConfigureAwait(false))
        {
            return 1;
        }

        string staging = Path.Combine(Path.GetTempPath(), $"fsh-swap-{Guid.NewGuid():N}");

        try
        {
            // Scaffold a throwaway copy under the SAME project name so every token the template
            // substitutes lands on the same values this project already uses.
            int scaffold = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse(FshConstants.AccentColor))
                .StartAsync("Generating kernel source from the template...", async _ =>
                {
                    var result = await ProcessRunner.CaptureWithErrorAsync(
                        "dotnet",
                        $"new {FshConstants.TemplateShortName} -n \"{project.Name}\" -o \"{staging}\" " +
                        "--aspire false --frontend false --skipRestore true --force",
                        cancellationToken: cancellationToken).ConfigureAwait(false);

                    if (!Directory.Exists(Path.Combine(staging, "src", "BuildingBlocks")))
                    {
                        foreach (string line in $"{result.output}\n{result.error}".Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)))
                            AnsiConsole.MarkupLine($"  [{FshConstants.DimColor}]{line.TrimEnd().EscapeMarkup()}[/]");
                        return 1;
                    }

                    return 0;
                }).ConfigureAwait(false);

            if (scaffold != 0)
            {
                AnsiConsole.MarkupLine($"[{FshConstants.ErrorColor}]Could not generate kernel source from the template.[/]");
                return 1;
            }

            ScaffoldedProject.CopyTree(Path.Combine(staging, "src", "BuildingBlocks"), project.BuildingBlocksPath);
            AnsiConsole.MarkupLine($"  [{FshConstants.SuccessColor}]added[/] src/BuildingBlocks");

            string stagedTests = Path.Combine(staging, "src", "Tests", "Framework.Tests");
            bool tests = Directory.Exists(stagedTests);
            if (tests)
            {
                ScaffoldedProject.CopyTree(stagedTests, project.FrameworkTestsPath);
                AnsiConsole.MarkupLine($"  [{FshConstants.SuccessColor}]added[/] src/Tests/Framework.Tests");
            }

            project.AddKernelToSolution(FshConstants.FrameworkProjects, tests);
            AnsiConsole.MarkupLine($"  [{FshConstants.SuccessColor}]updated[/] {Path.GetFileName(project.SolutionPath).EscapeMarkup()}");

            if (project.RemoveLocalFeedSource())
                AnsiConsole.MarkupLine($"  [{FshConstants.SuccessColor}]removed[/] NuGet.config (local framework feed no longer needed)");
        }
        finally
        {
            try { if (Directory.Exists(staging)) Directory.Delete(staging, recursive: true); }
            catch (IOException) { /* a leftover temp directory is not worth failing the swap over */ }
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[{FshConstants.SuccessColor}]Now on owned source.[/] Directory.Build.targets stops rewriting references as soon as");
        AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]src/BuildingBlocks exists, so nothing else needs changing. Build to confirm:[/]");
        AnsiConsole.MarkupLine($"  [{FshConstants.DimColor}]dotnet build src/{project.Name.EscapeMarkup()}.slnx[/]");
        return 0;
    }

    private static int SwapToPackages(ScaffoldedProject project, Settings settings)
    {
        if (!project.HasBuildingBlocksSource)
        {
            AnsiConsole.MarkupLine($"[{FshConstants.SuccessColor}]Already on packages[/] — there is no src/BuildingBlocks. Nothing to do.");
            return 0;
        }

        string feed = FrameworkFeed.Resolve(settings.Feed);
        string? version = settings.Version ?? FrameworkFeed.GetLatestVersion(feed);

        if (version is null)
        {
            AnsiConsole.MarkupLine($"[{FshConstants.ErrorColor}]No {FshConstants.FrameworkPackagePrefix}* packages in[/] {feed.EscapeMarkup()}");
            AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]Build them first, from a starter-kit clone: fsh framework pack --push[/]");
            return 1;
        }

        AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]Feed:[/] {feed.EscapeMarkup()}  [{FshConstants.DimColor}]Version:[/] {version.EscapeMarkup()}");

        if (settings.DryRun)
        {
            AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]Would delete src/BuildingBlocks and src/Tests/Framework.Tests, update the solution,[/]");
            AnsiConsole.MarkupLine($"[{FshConstants.DimColor}]write NuGet.config, and pin FshFrameworkVersion.[/]");
            return 0;
        }

        // Deleting the kernel source is the one irreversible step here, so it is confirmed by
        // default. `fsh framework swap --to source` can regenerate it, but any local edits to
        // BuildingBlocks would be gone.
        if (!settings.Yes)
        {
            AnsiConsole.MarkupLine($"[{FshConstants.WarningColor}]This deletes src/BuildingBlocks. Local edits to the kernel will be lost.[/]");

            if (!AnsiConsole.Confirm("Continue?", defaultValue: false))
                return 1;
        }

        Directory.Delete(project.BuildingBlocksPath, recursive: true);
        AnsiConsole.MarkupLine($"  [{FshConstants.SuccessColor}]removed[/] src/BuildingBlocks");

        if (Directory.Exists(project.FrameworkTestsPath))
        {
            // Framework.Tests references the kernel projects directly; leaving it behind would
            // break the build the moment the source is gone.
            Directory.Delete(project.FrameworkTestsPath, recursive: true);
            AnsiConsole.MarkupLine($"  [{FshConstants.SuccessColor}]removed[/] src/Tests/Framework.Tests");
        }

        project.RemoveKernelFromSolution();
        AnsiConsole.MarkupLine($"  [{FshConstants.SuccessColor}]updated[/] {Path.GetFileName(project.SolutionPath).EscapeMarkup()}");

        project.WriteNuGetConfig(feed);
        AnsiConsole.MarkupLine($"  [{FshConstants.SuccessColor}]wrote[/] NuGet.config");

        AnsiConsole.MarkupLine(project.SetFrameworkVersion(version)
            ? $"  [{FshConstants.SuccessColor}]pinned[/] FshFrameworkVersion = {version.EscapeMarkup()}"
            : $"  [{FshConstants.DimColor}]FshFrameworkVersion already {version.EscapeMarkup()}[/]");

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[{FshConstants.SuccessColor}]Now on framework packages.[/] Build to confirm:");
        AnsiConsole.MarkupLine($"  [{FshConstants.DimColor}]dotnet build src/{project.Name.EscapeMarkup()}.slnx[/]");
        return 0;
    }
}
